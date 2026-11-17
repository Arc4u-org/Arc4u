using Arc4u.OAuth2.TicketStore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// Configures the authentication cookie so the session is kept in an <see cref="ITicketStore"/>. It is registered (with <c>TryAddSingleton</c>) by
    /// <c>AddOidcAuthentication</c> and <c>AddHybridAuthentication</c> when <see cref="OidcAuthenticationOptions.AuthenticationCacheTicketStoreOption"/> is set;
    /// another <see cref="IPostConfigureOptions{TOptions}"/> of the cookie options registered before replaces it.
    /// </summary>
    public class ConfigureCookieWithTicketStoreAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly ITicketStore _ticketStore;
        private readonly IOptionsMonitor<OidcAuthenticationOptions> _options;
        private readonly ILogger _logger;

        /// <summary>Initializes a new instance of the <see cref="ConfigureCookieWithTicketStoreAuthenticationOptions"/> class.</summary>
        /// <param name="ticketStore">The store in which the authentication tickets are kept.</param>
        /// <param name="optionsMonitor">The OpenID Connect authentication options.</param>
        public ConfigureCookieWithTicketStoreAuthenticationOptions(ITicketStore ticketStore, IOptionsMonitor<OidcAuthenticationOptions> optionsMonitor)
            : this(ticketStore, optionsMonitor, NullLogger<ConfigureCookieWithTicketStoreAuthenticationOptions>.Instance)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ConfigureCookieWithTicketStoreAuthenticationOptions"/> class.</summary>
        /// <param name="ticketStore">The store in which the authentication tickets are kept.</param>
        /// <param name="optionsMonitor">The OpenID Connect authentication options.</param>
        /// <param name="logger">The logger warning about a cookie name without the <c>__Host-</c> prefix.</param>
        public ConfigureCookieWithTicketStoreAuthenticationOptions(ITicketStore ticketStore, IOptionsMonitor<OidcAuthenticationOptions> optionsMonitor, ILogger<ConfigureCookieWithTicketStoreAuthenticationOptions> logger)
        {
            _ticketStore = ticketStore;
            _options = optionsMonitor;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Configures the cookie: the ticket store keeps the session, the name comes from <see cref="OidcAuthenticationOptions.CookieName"/>, the expiration is the shorter of
        /// <see cref="OidcAuthenticationOptions.AuthenticationTicketTtl"/> and <see cref="OidcAuthenticationOptions.RefreshTokenLifetime"/> (sliding), and the cookie is essential, <c>SameSite=Lax</c>,
        /// secure, host-only and valid for the whole site (<c>Path=/</c>). A sign-in always starts a new session (see <see cref="FreshSessionCookieManager"/>).
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The cookie options to configure.</param>
        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            options.SessionStore = _ticketStore;
            AuthenticationCookieDefaults.Apply(options, _options.CurrentValue, _logger);
            // A sign-in must never renew the session key of a cookie the browser already holds (session fixation).
            options.CookieManager = options.CookieManager switch
            {
                FreshSessionCookieManager manager => manager,
                null => new FreshSessionCookieManager(new ChunkingCookieManager()),
                var other => new FreshSessionCookieManager(other),
            };
        }
    }
}
