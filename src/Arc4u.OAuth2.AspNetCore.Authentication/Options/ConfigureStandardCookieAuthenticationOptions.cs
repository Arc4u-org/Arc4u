using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// Configures the authentication cookie when no ticket store is used (the session is kept in the cookie). It is registered (with <c>TryAddSingleton</c>) by
    /// <c>AddOidcAuthentication</c> and <c>AddHybridAuthentication</c> when <see cref="OidcAuthenticationOptions.AuthenticationCacheTicketStoreOption"/> is not set.
    /// </summary>
    public class ConfigureStandardCookieAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly IOptionsMonitor<OidcAuthenticationOptions> _options;
        private readonly ILogger _logger;

        /// <summary>Initializes a new instance of the <see cref="ConfigureStandardCookieAuthenticationOptions"/> class.</summary>
        /// <param name="optionsMonitor">The OpenID Connect authentication options.</param>
        public ConfigureStandardCookieAuthenticationOptions(IOptionsMonitor<OidcAuthenticationOptions> optionsMonitor)
            : this(optionsMonitor, NullLogger<ConfigureStandardCookieAuthenticationOptions>.Instance)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ConfigureStandardCookieAuthenticationOptions"/> class.</summary>
        /// <param name="optionsMonitor">The OpenID Connect authentication options.</param>
        /// <param name="logger">The logger warning about a cookie name without the <c>__Host-</c> prefix.</param>
        public ConfigureStandardCookieAuthenticationOptions(IOptionsMonitor<OidcAuthenticationOptions> optionsMonitor, ILogger<ConfigureStandardCookieAuthenticationOptions> logger)
        {
            _options = optionsMonitor;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Configures the cookie: the name comes from <see cref="OidcAuthenticationOptions.CookieName"/>, the expiration is the shorter of
        /// <see cref="OidcAuthenticationOptions.AuthenticationTicketTtl"/> and <see cref="OidcAuthenticationOptions.RefreshTokenLifetime"/> (sliding), and the cookie is essential, <c>SameSite=Lax</c>,
        /// secure, host-only and valid for the whole site (<c>Path=/</c>).
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The cookie options to configure.</param>
        public void PostConfigure(string? name, CookieAuthenticationOptions options)
            => AuthenticationCookieDefaults.Apply(options, _options.CurrentValue, _logger);
    }
}
