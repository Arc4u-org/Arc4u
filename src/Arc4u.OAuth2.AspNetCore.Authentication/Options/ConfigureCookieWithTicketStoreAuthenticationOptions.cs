using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// This class is registered by default in the AuthenticationExtensions static class.
    /// This is possible to registe anothe one via the OidcAuthenticationBuilderOptions.
    /// </summary>
    public class ConfigureCookieWithTicketStoreAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly ITicketStore _ticketStore;
        private readonly IOptionsMonitor<OidcAuthenticationOptions> _options;

        /// <summary>Initializes a new instance of the <see cref="ConfigureCookieWithTicketStoreAuthenticationOptions"/> class.</summary>
        /// <param name="ticketStore">The store in which the authentication tickets are kept.</param>
        /// <param name="optionsMonitor">The OpenID Connect authentication options.</param>
        public ConfigureCookieWithTicketStoreAuthenticationOptions(ITicketStore ticketStore, IOptionsMonitor<OidcAuthenticationOptions> optionsMonitor)
        {
            _ticketStore = ticketStore;
            _options = optionsMonitor;
        }

        /// <summary>
        /// Configures the cookie: the ticket store keeps the session, the name comes from <see cref="OidcAuthenticationOptions.CookieName"/>, the expiration is the shorter of
        /// <see cref="OidcAuthenticationOptions.AuthenticationTicketTtl"/> and <see cref="OidcAuthenticationOptions.RefreshTokenLifetime"/> (sliding), and the cookie is essential, <c>SameSite=Lax</c> and secure.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The cookie options to configure.</param>
        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            options.SessionStore = _ticketStore;
            options.Cookie.Name = _options.CurrentValue.CookieName;
            options.SlidingExpiration = true;
            // Set the expiration time span to the minimum of AuthenticationTicketTTL and RefreshTokenLifetime
            // to avoid having a ticket that is expired but still valid.
            options.ExpireTimeSpan = _options.CurrentValue.AuthenticationTicketTtl < _options.CurrentValue.RefreshTokenLifetime
                ? _options.CurrentValue.AuthenticationTicketTtl
                : _options.CurrentValue.RefreshTokenLifetime;
            options.EventsType = typeof(CookieAuthenticationEvents);
            // we need this to persist the cookie and keep the user logged in.
            options.Cookie.IsEssential = true;
            // Need to set the same site to Lax to allow the cookie to be sent to the portal from the authority provider.
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.MaxAge = _options.CurrentValue.RefreshTokenLifetime;
        }
    }
}
