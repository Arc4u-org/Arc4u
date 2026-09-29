using System.Globalization;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Arc4u.OAuth2.Events
{
    /// <summary>
    /// The default <see cref="CookieAuthenticationEvents"/> of the OpenID Connect authentication. On each request it loads the access and refresh tokens stored in the cookie ticket into the scoped
    /// <see cref="TokenRefreshInfo"/> and, when the access token expires in less than <see cref="OidcAuthenticationOptions.ForceRefreshTimeoutTimeSpan"/>, refreshes it and renews the cookie.
    /// The principal is rejected and the user signed out when the cookie is expired or the token cannot be refreshed.
    /// </summary>
    public class StandardCookieEvents : CookieAuthenticationEvents
    {
        /// <summary>Initializes a new instance of the <see cref="StandardCookieEvents"/> class.</summary>
        /// <param name="serviceProvider">The service provider used to resolve the scoped <see cref="TokenRefreshInfo"/>.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="oidcOptions">The OpenID Connect authentication options.</param>
        /// <param name="tokenRefreshProvider">The provider that refreshes the tokens.</param>
        public StandardCookieEvents(IServiceProvider serviceProvider,
            ILogger<StandardCookieEvents> logger,
            IOptions<OidcAuthenticationOptions> oidcOptions,
            ITokenRefreshProvider tokenRefreshProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hybridOptions = oidcOptions.Value;
            _tokenRefreshProvider = tokenRefreshProvider;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly OidcAuthenticationOptions _hybridOptions;
        private readonly ITokenRefreshProvider _tokenRefreshProvider;
        private readonly ILogger<StandardCookieEvents> _logger;

        /// <inheritdoc/>
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext cookieCtx)
        {
            ArgumentNullException.ThrowIfNull(_serviceProvider);
            ArgumentNullException.ThrowIfNull(_hybridOptions);

            var cookieTokenExpiration = cookieCtx.Properties.ExpiresUtc;
            if (!cookieTokenExpiration.HasValue || cookieTokenExpiration.Value < DateTime.UtcNow)
            {
                _logger?.Technical().LogError("Cookie doesn't exist or is expired. Reject the principal and sign out the user.");
                cookieCtx.RejectPrincipal();
                await cookieCtx.HttpContext.SignOutAsync().ConfigureAwait(false);
                return;
            }

            // => must be defined in the options
            var refreshThreshold = _hybridOptions.ForceRefreshTimeoutTimeSpan;

            _logger.Technical().LogExtractCookieFromTokenCache();

            // Persist the Access and Refresh tokens.
            // TokenRefreshInfo is registered as Scoped and we create at this moment (by request) an instance to
            // set the information stored in the TicketStore repo based on the cookie information.

            var tokensInfo = _serviceProvider.GetService<TokenRefreshInfo>();

            if (null == tokensInfo)
            {
                _logger.Technical().LogNoTokenRefreshInfo();
                cookieCtx.RejectPrincipal();
                await cookieCtx.HttpContext.SignOutAsync().ConfigureAwait(false);
                return;
            }

            var accessToken = cookieCtx.Properties.GetTokenValue("access_token");
            var jwtToken = new JsonWebToken(accessToken);
            var refreshToken = cookieCtx.Properties.GetTokenValue("refresh_token");

            tokensInfo.AccessToken = new TokenInfo("access_token", accessToken ?? string.Empty, jwtToken.ValidTo);
            // As not all the autorities are using a jwt token for the refresh token, the expiration date is not  extracted from the token
            tokensInfo.RefreshToken = new TokenInfo("refresh_token", refreshToken ?? string.Empty, DateTime.UtcNow + _hybridOptions.RefreshTokenLifetime);

            var timeRemaining = jwtToken.ValidTo - DateTime.UtcNow;
            if (timeRemaining < refreshThreshold)
            {
                ArgumentNullException.ThrowIfNull(_tokenRefreshProvider);
                try
                {
                    if (timeRemaining < TimeSpan.Zero)
                    {
                        _logger?.Technical().LogAccessTokenIsExpired(timeRemaining.Multiply(-1));
                    }
                    else
                    {
                        _logger?.Technical().LogAccessTokenIsExpiring(timeRemaining);
                    }

                    // throws an exception if the call failed.
                    await _tokenRefreshProvider.RefreshTokenAsync(CancellationToken.None).ConfigureAwait(false);

                    cookieCtx.Properties.UpdateTokenValue("access_token", tokensInfo.AccessToken.Token);
                    cookieCtx.Properties.UpdateTokenValue("refresh_token", tokensInfo.RefreshToken.Token);
                    cookieCtx.Properties.UpdateTokenValue("expires_at", tokensInfo.AccessToken.ExpiresOnUtc.ToString("o", CultureInfo.InvariantCulture));

                    cookieCtx.ShouldRenew = true;
                    _logger?.Technical().LogDebug("Renew the cookie.");
                }
                catch (Exception ex)
                {
                    _logger?.Technical().LogCantRefreshToken();
                    _logger?.Technical().LogException(ex);

                    cookieCtx.RejectPrincipal();
                    await cookieCtx.HttpContext.SignOutAsync().ConfigureAwait(false);
                }

            }
        }
    }
}

