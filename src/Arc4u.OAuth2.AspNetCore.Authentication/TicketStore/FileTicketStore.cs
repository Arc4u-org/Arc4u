using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>An <see cref="ITicketStore"/> that keeps each authentication ticket in a file (<c>{guid}.bin</c>) of the directory configured in <see cref="FileTicketStoreOptions.StorePath"/>. It is meant for a single instance application.</summary>
    public class FileTicketStore : ITicketStore
    {
        /// <summary>Initializes a new instance of the <see cref="FileTicketStore"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options giving the directory of the tickets.</param>
        /// <exception cref="ArgumentNullException">The store path is <see langword="null"/>.</exception>
        public FileTicketStore(ILogger<FileTicketStore> logger, IOptionsMonitor<FileTicketStoreOptions> options)
        {
            _logger = logger;

            ArgumentNullException.ThrowIfNull(options.CurrentValue.StorePath);

            _directoryStore = options.CurrentValue.StorePath;
        }

        private readonly DirectoryInfo _directoryStore;
        private readonly object _lock = new();
        private readonly ILogger<FileTicketStore> _logger;
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
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            await RemoveAsync(key).ConfigureAwait(false);
            var fullPath = GetPath(key);

            lock (_lock)
            {
                File.WriteAllBytes(fullPath, TicketSerializer.Default.Serialize(ticket));
                _logger.Technical().LogRenewAuthenticationTicket(key,fullPath);
            }

            return;
        }

        /// <inheritdoc/>
        public Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var fullPath = GetPath(key);

            AuthenticationTicket? ticket = null;

            if (File.Exists(fullPath))
            {
                var content = File.ReadAllBytes(fullPath);

                ticket = TicketSerializer.Default.Deserialize(content);
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
                File.WriteAllBytes(fullPath, TicketSerializer.Default.Serialize(ticket));
                _logger.Technical().LogCreateAuthenticationTicketOnFile(key, fullPath);
            }

            return Task.FromResult(key);
        }
    }
}
