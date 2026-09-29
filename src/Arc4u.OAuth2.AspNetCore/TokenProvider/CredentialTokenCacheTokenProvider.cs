using System.Globalization;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>
/// An <see cref="ITokenProvider"/> for user name and password credentials. The token is cached (key: authority, scope, user and a hash of the password) and requested again
/// through the <see cref="ICredentialTokenProvider"/> registered as <c>CredentialDirect</c> when it is missing or expires in less than one minute.
/// </summary>
/// <param name="tokenCache">The token cache.</param>
/// <param name="logger">The logger.</param>
/// <param name="container">The service provider used to resolve the <see cref="ICredentialTokenProvider"/>.</param>
/// <param name="authorities">The named authority options.</param>
[Export(CredentialTokenCacheTokenProvider.ProviderName, typeof(ITokenProvider))]
public class CredentialTokenCacheTokenProvider(ITokenCache tokenCache, ILogger<CredentialTokenCacheTokenProvider> logger, IServiceProvider container, IOptionsMonitor<AuthorityOptions> authorities) : ITokenProvider
{
    /// <summary>The key (<c>Credential</c>) under which the provider is registered.</summary>
    public const string ProviderName = "Credential";

    private const string User = "User";
    private const string Password = "Password";

    /// <summary>
    /// Acquires a token for a user/password credential, caching it by user + authority + scope.
    /// <para>
    /// The credential is taken from <paramref name="platformParameters"/> when it is a
    /// <see cref="CredentialsResult"/> (the path used by <c>BasicAuthenticationMiddleware</c>, which
    /// extracts it from the request); otherwise it is built from the <c>User</c>/<c>Password</c>
    /// keys present in <paramref name="settings"/> (the client-token scenario path).
    /// </para>
    /// </summary>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var credential = platformParameters as CredentialsResult ?? BuildCredential(settings);

        return await GetTokenAsync(settings, credential).ConfigureAwait(false);
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

    private static CredentialsResult BuildCredential(IKeyValueSettings settings)
    {
        var hasUser = settings.Values.TryGetValue(User, out var user);
        var hasPassword = settings.Values.TryGetValue(Password, out var password);

        return new CredentialsResult(hasUser && hasPassword, user, password);
    }

    private async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings settings, CredentialsResult credential)
    {
        var result = GetContext(settings, out var authority, out var scope);

        if (string.IsNullOrWhiteSpace(credential.Upn))
        {
            result.WithError("No Username is provided.");
        }

        if (string.IsNullOrWhiteSpace(credential.Password))
        {
            result.WithError("No password is provided.");
        }

        if (result.IsFailed)
        {
            return result;
        }

        if (null != tokenCache)
        {
            Result<TokenInfo> tokenInfoResult;

            // Get a HashCode from the password so a second call with the same upn but with a wrong password will not be impersonated due to
            // the lack of password check.
            // authority is not null here => messages log and throw will throw an exception if null.
            var cacheKey = BuildKey(credential, authority!, scope);

            logger.Technical().LogCheckContainsKeyInCache(cacheKey);
            var tokenInfo = tokenCache.Get<TokenInfo>(cacheKey);
            var hasChanged = false;

            if (null != tokenInfo)
            {
                tokenInfoResult = Result.Ok(tokenInfo);
                logger.Technical().LogTokenLoadedFromCache(cacheKey);

                if (tokenInfo.ExpiresOnUtc < DateTime.UtcNow.AddMinutes(1))
                {
                    logger.Technical().LogTokenExpiredLoadedFromCache(cacheKey);

                    // We need to refresh the token.
                    tokenInfoResult = await CreateBasicTokenInfoAsync(settings, credential).ConfigureAwait(false);
                    hasChanged = true;
                }
            }
            else
            {
                logger.Technical().LogCallSTS(cacheKey);
                tokenInfoResult = await CreateBasicTokenInfoAsync(settings, credential).ConfigureAwait(false);
                hasChanged = true;
            }

            if (hasChanged)
            {
                try
                {
                    logger.Technical().LogSaveTokenInCache(cacheKey, tokenInfoResult.Value.ExpiresOnUtc);
                    tokenCache.Put(cacheKey, tokenInfoResult.Value);
                }
                catch (Exception ex)
                {
                    tokenInfoResult.WithError(new ExceptionalError(ex));
                }
            }

            return tokenInfoResult;
        }

        // no cache, do a direct call on every calls.
        logger.Technical().LogNoCachePerformance();
        return await CreateBasicTokenInfoAsync(settings, credential).ConfigureAwait(false);

    }

    /// <summary>Requests a token for the credentials from the <c>CredentialDirect</c> <see cref="ICredentialTokenProvider"/>, without using the cache.</summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="credential">The user credentials.</param>
    /// <returns>The token, or a failed result when the credential token provider is not registered or the request fails.</returns>
    protected async Task<Result<TokenInfo>> CreateBasicTokenInfoAsync(IKeyValueSettings settings, CredentialsResult credential)
    {
        var basicTokenProvider = container.GetKeyedService<ICredentialTokenProvider>(CredentialTokenProvider.ProviderName);

        if (basicTokenProvider == null)
        {
            logger.Technical().LogNoTokenProvider(CredentialTokenProvider.ProviderName);
            return Result.Fail($"No token provider found for {CredentialTokenProvider.ProviderName}.");
        }

        return await basicTokenProvider.GetTokenAsync(settings, credential).ConfigureAwait(false);
    }

    private static string BuildKey(CredentialsResult credential, AuthorityOptions authority, string audience)
    {
        if (null == credential?.Password)
        {
            throw new InvalidOperationException("The password cannot be null.");
        }
        return authority.Url + "_" + audience + "_Password_" + credential.Upn + "_" + credential.Password.GetHashCode().ToString(CultureInfo.InvariantCulture);
    }

    private Result GetContext(IKeyValueSettings settings, out AuthorityOptions? authority, out string scope)
    {
        // Check the information.
        var result = new Result();

        if (null == settings)
        {
            authority = null;
            scope = string.Empty;

            return Result.Fail("Settings parameter cannot be null.");
        }

        // Valdate arguments.
        if (!settings.Values.ContainsKey(TokenKeys.Scope))
        {
            result.WithError("Scope is missing. Cannot process the request.");
        }

        logger.Technical().LogCreateAuthenticationContext();

        if (!settings.Values.ContainsKey(TokenKeys.AuthorityKey))
        {
            authority = authorities.Get("Default");
        }
        else
        {
            authority = authorities.Get(settings.Values[TokenKeys.AuthorityKey]);
        }
        scope = settings.Values[TokenKeys.Scope];

        return result;
    }
}
