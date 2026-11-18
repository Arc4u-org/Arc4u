using Arc4u.Caching;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>
    /// An <see cref="ITicketStore"/> that keeps the authentication tickets in an Arc4u cache. Each ticket is stored under a key made of
    /// <see cref="CacheTicketStoreOptions.KeyPrefix"/> and a new guid, encrypted with Data Protection (purpose <see cref="CacheTicketStoreOptions.DataProtectionPurpose"/>):
    /// a ticket carries the user's access, id and refresh tokens, which must not be readable by whoever can read the cache.
    /// </summary>
    /// <remarks>
    /// An entry that cannot be unprotected (written in clear by a previous version, or under a key ring that no longer exists) reads as no
    /// ticket: the user signs in again, and the entry is removed so its tokens leave the cache at once.
    /// </remarks>
    public class CacheTicketStore : ITicketStore
    {
        /// <summary>Initializes a new instance of the <see cref="CacheTicketStore"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="cacheContext">The context giving access to the registered caches.</param>
        /// <param name="options">The options giving the name of the cache and the key prefix. When the cache does not exist, the default cache is used.</param>
        /// <param name="dataProtectionProvider">The Data Protection provider that encrypts the tickets. All the instances of the application must share its key ring.</param>
        /// <exception cref="ArgumentNullException">The cache name, the key prefix or <paramref name="dataProtectionProvider"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The Data Protection purpose is empty.</exception>
        public CacheTicketStore(ILogger<CacheTicketStore> logger, ICacheContext cacheContext, IOptionsMonitor<CacheTicketStoreOptions> options, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);
            ArgumentNullException.ThrowIfNull(options.CurrentValue.KeyPrefix);
            ArgumentNullException.ThrowIfNull(options.CurrentValue.CacheName);
            var dataProtectionPurpose = options.CurrentValue.DataProtectionPurpose;
            ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionPurpose);

            _logger = logger;
            _cacheContext = cacheContext;
            _cache = GetCache(options.CurrentValue.CacheName);
            _keyPrefix = options.CurrentValue.KeyPrefix;
            _protector = dataProtectionProvider.CreateProtector(dataProtectionPurpose);
        }

        private readonly ILogger<CacheTicketStore> _logger;
        private readonly ICacheContext _cacheContext;
        private readonly ICache _cache;
        private readonly string _keyPrefix;
        private readonly IDataProtector _protector;

        private ICache GetCache(string key)
        {
            if (_cacheContext.Exist(key))
            {
                _logger.Technical().LogCacheNameUsed(key);
                return _cacheContext[key];
            }

            _logger.Technical().LogNoCacheExist(key);
            return _cacheContext.Default;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key).ConfigureAwait(false);

            _logger.Technical().LogDeleteAuthenticationTicket(key);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The session <paramref name="key"/> holds the ticket of another user; the stored ticket is left unchanged.</exception>
        /// <remarks>The ticket of another user is never overwritten: that would hand that session to whoever holds the cookie (session fixation).</remarks>
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket);

            var stored = await TryGetStoredTicketAsync(key).ConfigureAwait(false);
            if (stored is not null && !AuthenticationTicketSubject.IsSameSubject(stored, ticket))
            {
                _logger.Technical().LogSessionSubjectMismatch(key);
                throw new InvalidOperationException("The session belongs to another user: its authentication ticket is not overwritten.");
            }

            await WriteAsync(key, ticket).ConfigureAwait(false);
        }

        // Reads the stored ticket without logging a missing one as an error: an expired session is expected here. An entry that cannot be
        // unprotected holds no ticket to compare with, like a missing one; the renewal then replaces it with an encrypted ticket.
        private async Task<AuthenticationTicket?> TryGetStoredTicketAsync(string key)
        {
            try
            {
                var content = await _cache.GetAsync<byte[]>(key).ConfigureAwait(false);
                return content is not null && TicketStoreDataProtection.TryUnprotect(_protector, content, out var ticket) ? ticket : null;
            }
            catch (DataCacheException)
            {
                return null;
            }
        }

        private async Task WriteAsync(string key, AuthenticationTicket ticket)
        {
            var expiresUtc = ticket.Properties.ExpiresUtc;
            if (expiresUtc.HasValue)
            {
                var time = expiresUtc - DateTime.UtcNow ?? TimeSpan.FromHours(4);
                await _cache.PutAsync<byte[]>(key, time, TicketStoreDataProtection.Protect(_protector, ticket)).ConfigureAwait(false);
                _logger.Technical().LogCreateAuthenticationTicket(key, time);
                return;
            }
            await _cache.PutAsync(key, TimeSpan.FromHours(4), TicketStoreDataProtection.Protect(_protector, ticket)).ConfigureAwait(false);
            _logger.Technical().LogAuthenticationTicketCreated(key);

        }

        /// <inheritdoc/>
        public async Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            try
            {
                var content = await _cache.GetAsync<byte[]>(key).ConfigureAwait(false);

                if (content is null)
                {
                    // Expected after a sign-out or an expiry (for example on each circuit revalidation): not an error.
                    _logger.Technical().LogNoAuthenticationTicketForKey(key);
                    return null;
                }

                if (!TicketStoreDataProtection.TryUnprotect(_protector, content, out var ticket))
                {
                    _logger.Technical().LogTicketCannotBeUnprotected(key);
                    await TryRemoveUnreadableAsync(key).ConfigureAwait(false);
                    return null;
                }

                if (ticket is null)
                {
                    _logger.Technical().LogAuthenticationTicketIsNull();
                }

                return ticket;
            }
            catch (DataCacheException)
            {
                _logger.Technical().LogNoAuthenticationTicketFromCache();

                return null;
            }
        }

        // Best effort: the entry still expires on its own, and the request must not fail over it.
        private async Task TryRemoveUnreadableAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key).ConfigureAwait(false);
            }
            catch (DataCacheException ex)
            {
                _logger.Technical().LogUnreadableTicketNotRemoved(ex, key);
            }
        }

        /// <inheritdoc/>
        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = _keyPrefix + Guid.NewGuid().ToString();
            // A new key holds no ticket yet: nothing to compare with.
            await WriteAsync(key, ticket).ConfigureAwait(false);

            return key;
        }
    }
}
