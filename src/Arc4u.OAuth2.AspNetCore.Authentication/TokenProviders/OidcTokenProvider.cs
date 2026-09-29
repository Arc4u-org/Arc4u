using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TokenProviders
{
    /// <summary>
    /// An <see cref="ITokenProvider"/> that returns the access token of a user authenticated with OpenID Connect and the cookie scheme.
    /// The token comes from the scoped <see cref="TokenRefreshInfo"/>; when it expires in less than <see cref="OidcAuthenticationOptions.ForceRefreshTimeoutTimeSpan"/>, it is refreshed with the refresh token first.
    /// </summary>
    [Export(OidcTokenProvider.ProviderName, typeof(ITokenProvider))]
    public class OidcTokenProvider : ITokenProvider
    {
        /// <summary>The key (<c>Oidc</c>) under which the provider is registered.</summary>
        public const string ProviderName = "Oidc";

        /// <summary>Initializes a new instance of the <see cref="OidcTokenProvider"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="tokenRefreshInfo">The scoped access and refresh tokens of the user.</param>
        /// <param name="oidcOptions">The OpenID Connect authentication options.</param>
        /// <param name="refreshTokenProvider">The provider that refreshes the tokens.</param>
        public OidcTokenProvider(ILogger<OidcTokenProvider> logger, TokenRefreshInfo tokenRefreshInfo, IOptions<OidcAuthenticationOptions> oidcOptions, ITokenRefreshProvider refreshTokenProvider)
        {
            _logger = logger;
            _tokenRefreshInfo = tokenRefreshInfo;
            _hybridOptions = oidcOptions.Value;
            _refreshTokenProvider = refreshTokenProvider;
        }

        private readonly ILogger<OidcTokenProvider> _logger;
        private readonly TokenRefreshInfo _tokenRefreshInfo;
        private readonly OidcAuthenticationOptions _hybridOptions;
        private readonly ITokenRefreshProvider _refreshTokenProvider;

        /// <summary>Gets the access token of the user, refreshing it when it is about to expire.</summary>
        /// <param name="settings">The provider settings, which must not be <see langword="null"/>.</param>
        /// <param name="platformParameters">Not used.</param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <returns>The access token.</returns>
        public async Task<Result<TokenInfo?>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var timeRemaining = _tokenRefreshInfo.AccessToken.ExpiresOnUtc.Subtract(DateTime.UtcNow);

            if (timeRemaining > _hybridOptions.ForceRefreshTimeoutTimeSpan)
            {
                return _tokenRefreshInfo.AccessToken;
            }

            var refreshTokenInfo = await _refreshTokenProvider.RefreshTokenAsync(CancellationToken.None).ConfigureAwait(false);

            return refreshTokenInfo?.AccessToken;
        }

        /// <summary>Not supported.</summary>
        /// <param name="settings">The provider settings.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Never returns.</returns>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
