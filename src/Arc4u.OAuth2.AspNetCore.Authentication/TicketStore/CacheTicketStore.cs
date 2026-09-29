using Arc4u.Caching;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>An <see cref="ITicketStore"/> that keeps the authentication tickets in an Arc4u cache. Each ticket is stored under a key made of <see cref="CacheTicketStoreOptions.KeyPrefix"/> and a new guid.</summary>
    public class CacheTicketStore : ITicketStore
    {
        /// <summary>Initializes a new instance of the <see cref="CacheTicketStore"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="cacheContext">The context giving access to the registered caches.</param>
        /// <param name="options">The options giving the name of the cache and the key prefix. When the cache does not exist, the default cache is used.</param>
        /// <exception cref="ArgumentNullException">The cache name or the key prefix is <see langword="null"/>.</exception>
        public CacheTicketStore(ILogger<CacheTicketStore> logger, ICacheContext cacheContext, IOptionsMonitor<CacheTicketStoreOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options.CurrentValue.KeyPrefix);
            ArgumentNullException.ThrowIfNull(options.CurrentValue.CacheName);

            _logger = logger;
            _cacheContext = cacheContext;
            _cache = GetCache(options.CurrentValue.CacheName);
            _keyPrefix = options.CurrentValue.KeyPrefix;
        }

        private readonly ILogger<CacheTicketStore> _logger;
        private readonly ICacheContext _cacheContext;
        private readonly ICache _cache;
        private readonly string _keyPrefix;

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
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var expiresUtc = ticket.Properties.ExpiresUtc;
            if (expiresUtc.HasValue)
            {
                var time = expiresUtc - DateTime.UtcNow ?? TimeSpan.FromHours(4);
                await _cache.PutAsync<byte[]>(key, time, TicketSerializer.Default.Serialize(ticket)).ConfigureAwait(false);
                _logger.Technical().LogCreateAuthenticationTicket(key, time);
                return;
            }
            await _cache.PutAsync(key, TimeSpan.FromHours(4), TicketSerializer.Default.Serialize(ticket)).ConfigureAwait(false);
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
                    _logger.Technical().LogError($"No Authentication ticket from the cache with key {key}.");
                    return null;
                }

                var ticket = TicketSerializer.Default.Deserialize(content);
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

        /// <inheritdoc/>
        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = _keyPrefix + Guid.NewGuid().ToString();
            await RenewAsync(key, ticket).ConfigureAwait(false);

            return key;
        }
    }
}
