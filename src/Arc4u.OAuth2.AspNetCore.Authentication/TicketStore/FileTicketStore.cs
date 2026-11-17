using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>
    /// An <see cref="ITicketStore"/> that keeps each authentication ticket in a file (<c>{guid}.bin</c>) of the directory configured in
    /// <see cref="FileTicketStoreOptions.StorePath"/>, encrypted with Data Protection (purpose <see cref="FileTicketStoreOptions.DataProtectionPurpose"/>):
    /// a ticket carries the user's access, id and refresh tokens, which must not be readable by whoever can read the directory. It is meant
    /// for a single instance application.
    /// </summary>
    /// <remarks>
    /// A file that cannot be unprotected (written in clear by a previous version, or under a key ring that no longer exists) reads as no
    /// ticket: the user signs in again, and the file is deleted.
    /// </remarks>
    public class FileTicketStore : ITicketStore
    {
        /// <summary>Initializes a new instance of the <see cref="FileTicketStore"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options giving the directory of the tickets.</param>
        /// <param name="dataProtectionProvider">The Data Protection provider that encrypts the tickets.</param>
        /// <exception cref="ArgumentNullException">The store path or <paramref name="dataProtectionProvider"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The Data Protection purpose is empty.</exception>
        public FileTicketStore(ILogger<FileTicketStore> logger, IOptionsMonitor<FileTicketStoreOptions> options, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.CurrentValue.DataProtectionPurpose, "options.DataProtectionPurpose");
            _logger = logger;
            _protector = dataProtectionProvider.CreateProtector(options.CurrentValue.DataProtectionPurpose);

            ArgumentNullException.ThrowIfNull(options.CurrentValue.StorePath);

            _directoryStore = options.CurrentValue.StorePath;
        }

        private readonly DirectoryInfo _directoryStore;
        private readonly object _lock = new();
        private readonly ILogger<FileTicketStore> _logger;
        private readonly IDataProtector _protector;
        /// <inheritdoc/>
        public Task RemoveAsync(string key)
        {
            var fullPath = GetPath(key);

            lock (_lock)
            {
                File.Delete(fullPath);
                _logger.Technical().LogRemoveAuthenticationTicket(key);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The session <paramref name="key"/> holds the ticket of another user; the stored ticket is left unchanged.</exception>
        /// <remarks>The ticket of another user is never overwritten: that would hand that session to whoever holds the cookie (session fixation).</remarks>
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket);

            var stored = await RetrieveAsync(key).ConfigureAwait(false);
            if (stored is not null && !AuthenticationTicketSubject.IsSameSubject(stored, ticket))
            {
                _logger.Technical().LogSessionSubjectMismatch(key);
                throw new InvalidOperationException("The session belongs to another user: its authentication ticket is not overwritten.");
            }

            await RemoveAsync(key).ConfigureAwait(false);
            var fullPath = GetPath(key);

            lock (_lock)
            {
                File.WriteAllBytes(fullPath, TicketStoreDataProtection.Protect(_protector, ticket));
                _logger.Technical().LogRenewAuthenticationTicket(key,fullPath);
            }
        }

        /// <inheritdoc/>
        public Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var fullPath = GetPath(key);

            AuthenticationTicket? ticket = null;

            if (File.Exists(fullPath))
            {
                var content = File.ReadAllBytes(fullPath);

                if (!TicketStoreDataProtection.TryUnprotect(_protector, content, out ticket))
                {
                    _logger.Technical().LogTicketCannotBeUnprotected(key);
                    lock (_lock)
                    {
                        File.Delete(fullPath);
                    }

                    return Task.FromResult<AuthenticationTicket?>(null);
                }

                _logger.Technical().LogGetAuthenticationTicket(key, fullPath);
            }
            else
            {
                _logger.Technical().LogNoFileExistForAuthenticationTicket(fullPath);
            }

            return Task.FromResult(ticket);
        }

        private string GetPath(string key) => Path.Combine(_directoryStore.FullName, key + ".bin");

        /// <inheritdoc/>
        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString();
            var fullPath = GetPath(key);

            lock (_lock)
            {
                File.WriteAllBytes(fullPath, TicketStoreDataProtection.Protect(_protector, ticket));
                _logger.Technical().LogCreateAuthenticationTicketOnFile(key, fullPath);
            }

            return Task.FromResult(key);
        }
    }
}
