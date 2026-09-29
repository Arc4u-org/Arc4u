using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using FluentResults;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Client.Authentication.TokenProvider;

/// <summary>
/// An <see cref="ITokenProvider"/> for a client application using <c>Duende.IdentityModel.OidcClient</c>. The tokens are kept in the secure cache; an expired access token is renewed with the refresh token,
/// and the user is logged in through the browser when there is no valid refresh token. The refresh token is assumed valid for 90 days.
/// </summary>
/// <param name="options">The <c>OidcClient</c> options.</param>
/// <param name="tokensInfo">The access and refresh tokens of the user.</param>
/// <param name="secureCache">The secure cache in which the tokens are persisted.</param>
/// <param name="apiExtraContextOption">The extra parameters added to the authorization and token requests.</param>
[Export(TokenProviderName, typeof(ITokenProvider))]
public class OidcClientIdentityModelTokenProvider(
                                                    OidcClientOptions options,
                                                    TokenRefreshInfo tokensInfo,
                                                    ISecureCache secureCache,
                                                    IOptionsMonitor<ApiExtraContextAuthenticationOption> apiExtraContextOption) : ITokenProvider
{
    /// <summary>The key (<c>OidcClientIdentityModel</c>) under which the provider is registered.</summary>
    public const string TokenProviderName = "OidcClientIdentityModel";
    private const string TokenKey = "TokensInfo";
    private const string TokenType = "Bearer";
    /// <summary>Gets the access token of the user, refreshing it or logging the user in when necessary.</summary>
    /// <param name="settings">The provider settings. They are not used.</param>
    /// <param name="platformParameters">Not used.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="Exception">The refresh of the token failed.</exception>
    /// <exception cref="AccessViolationException">The login failed.</exception>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        // Check if the token exists?
        if (null == tokensInfo.AccessToken)
        {
            var tokens = await secureCache.GetAsync<TokenRefreshInfo>(TokenKey).ConfigureAwait(false) ??  new TokenRefreshInfo();
            tokensInfo.AccessToken = tokens.AccessToken;
            tokensInfo.RefreshToken = tokens.RefreshToken;
        }

        if (tokensInfo.AccessToken is not null && tokensInfo.AccessToken.ExpiresOnUtc > DateTime.UtcNow)
        {
            return tokensInfo.AccessToken;
        }

        var client = new OidcClient(options);

        if (tokensInfo.RefreshToken is not null && tokensInfo.RefreshToken.ExpiresOnUtc > DateTime.UtcNow)
        {
            var parameters = new Parameters();
            foreach (var parameter in apiExtraContextOption.CurrentValue.AuthorizationParameters.Values)
            {
                parameters.Add(parameter.Key, parameter.Value);
            }
            foreach (var parameter in apiExtraContextOption.CurrentValue.TokenParameters.Values)
            {
                parameters.Add(parameter.Key, parameter.Value);
            }

            var result = await client.RefreshTokenAsync(tokensInfo.RefreshToken.Token, parameters).ConfigureAwait(false);

            if (result.IsError)
            {
                throw new Exception(result.Error);
            }

            tokensInfo.RefreshToken = new TokenInfo(TokenType, result.RefreshToken, DateTime.UtcNow.AddDays(90));
            tokensInfo.AccessToken = new TokenInfo(TokenType, result.AccessToken, result.AccessTokenExpiration.UtcDateTime);
            await secureCache.PutAsync(TokenKey, tokensInfo).ConfigureAwait(false);

            return tokensInfo.AccessToken;
        }

        // Perform the login.
        var loginRequest = new LoginRequest();
        foreach (var parameter in apiExtraContextOption.CurrentValue.AuthorizationParameters.Values)
        {
            loginRequest.BackChannelExtraParameters.Add(parameter.Key, parameter.Value);
        }
        foreach (var parameter in apiExtraContextOption.CurrentValue.TokenParameters.Values)
        {
            loginRequest.FrontChannelExtraParameters.Add(parameter.Key, parameter.Value);
        }
        var loginResult = await client.LoginAsync(loginRequest, CancellationToken.None).ConfigureAwait(false);

        if (loginResult.IsError)
        {
            throw new AccessViolationException(loginResult.Error);
        }

        tokensInfo.RefreshToken = new TokenInfo(TokenType, loginResult.RefreshToken, DateTime.UtcNow.AddDays(90));
        tokensInfo.AccessToken = new TokenInfo(TokenType, loginResult.AccessToken, loginResult.AccessTokenExpiration.UtcDateTime);
        await secureCache.PutAsync("TokensInfo", tokensInfo).ConfigureAwait(false);

        return tokensInfo.AccessToken;
    }

    /// <summary>Removes the tokens from the secure cache and logs the user out of the authority.</summary>
    /// <param name="settings">The provider settings, which are not used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is logged out.</returns>
    public async ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        await secureCache.RemoveAsync(TokenKey, cancellationToken).ConfigureAwait(false);

        var client = new OidcClient(options);

        await client.LogoutAsync(null, cancellationToken).ConfigureAwait(false);
    }
}
