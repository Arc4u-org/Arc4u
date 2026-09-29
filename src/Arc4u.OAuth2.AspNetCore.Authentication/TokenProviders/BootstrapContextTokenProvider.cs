using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;

namespace Arc4u.OAuth2.TokenProviders
{
    /// <summary>An <see cref="ITokenProvider"/> that returns the access token stored in the <see cref="ClaimsIdentity.BootstrapContext"/> of the current principal, that is the token of the incoming request.</summary>
    /// <param name="applicationContext">The application context giving the current principal.</param>
    [Export(BootstrapContextTokenProvider.ProviderName, typeof(ITokenProvider))]
    public class BootstrapContextTokenProvider(IApplicationContext applicationContext) : ITokenProvider
    {
        /// <summary>The key (<c>Bootstrap</c>) under which the provider is registered.</summary>
        public const string ProviderName = "Bootstrap";

        /// <summary>Gets the token of the current principal.</summary>
        /// <param name="settings">The provider settings, which must not be <see langword="null"/>.</param>
        /// <param name="platformParameters">Not used.</param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> or the current principal is <see langword="null"/>.</exception>
        /// <returns>The token, or a failed result when the identity has no token or the token is expired.</returns>
        public Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
        {
            ArgumentNullException.ThrowIfNull(settings);

            ArgumentNullException.ThrowIfNull(applicationContext.Principal);

            if (applicationContext.Principal.Identity is ClaimsIdentity identity && !string.IsNullOrWhiteSpace(identity?.BootstrapContext?.ToString()))
            {
                var token = identity.BootstrapContext.ToString();

                JwtSecurityToken jwt = new(token);

                if (jwt.ValidTo > DateTime.UtcNow)
                {
                    return Task.FromResult(new TokenInfo("Bearer", token!, jwt.ValidTo).ToResult());
                }

                return Task.FromResult<Result<TokenInfo>>(Result.Fail("The token provided is expired."));
            }

            return Task.FromResult<Result<TokenInfo>>(Result.Fail("No Access token stored in the Identity."));
        }

        /// <summary>
        /// Not supported: there is no way to sign out in this scenario.
        /// </summary>
        /// <param name="settings">The provider settings.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

