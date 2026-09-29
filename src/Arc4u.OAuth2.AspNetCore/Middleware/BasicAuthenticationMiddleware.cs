using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Middleware;

/// <summary>
/// A middleware that converts credentials into a bearer token. The credentials are read from a Basic <c>Authorization</c> header or, when the
/// <see cref="BasicAuthenticationSettingsOptions.CertificateHeaderOptions"/> are configured, from an encrypted header or query string parameter.
/// A token is requested for them from the token provider designated by the <c>ProviderId</c> of the settings, cached in the <see cref="ITokenCache"/>,
/// and the <c>Authorization</c> header of the request is replaced by <c>Bearer &lt;token&gt;</c>.
/// Errors are logged and the request continues without change.
/// </summary>
public class BasicAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BasicAuthenticationSettingsOptions _options;
    private readonly ILogger<BasicAuthenticationMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="BasicAuthenticationMiddleware"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The Basic authentication settings.</param>
    /// <param name="serviceProvider">The service provider used to check that the token provider is registered.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">The settings are missing, have no <c>ProviderId</c> or designate a token provider that is not registered.</exception>
    public BasicAuthenticationMiddleware(RequestDelegate next,
                                         ILogger<BasicAuthenticationMiddleware> logger,
                                         IOptionsMonitor<BasicAuthenticationSettingsOptions> options,
                                         IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;

        _options = options.CurrentValue;

        if (_options.BasicSettings is null)
        {
            throw new ConfigurationException("Settings collection for basic authentication cannot be null");
        }

        if (_options.BasicSettings.Values.ContainsKey(TokenKeys.ProviderIdKey))
        {
            if (!serviceProvider.TryGetService<ITokenProvider>(_options.BasicSettings.Values[TokenKeys.ProviderIdKey], out _))
            {
                throw new ConfigurationException($"No token provider ITokenProvider is defined with ProviderId {_options.BasicSettings.Values[TokenKeys.ProviderIdKey]}!");
            }
        }
        else
        {
            throw new ConfigurationException("No token provider resolution name is defined in your settings!");
        }
        if (!serviceProvider.TryGetService<ITokenCache>(out _))
        {
            _logger.Technical().LogError($"No token cache is defined for Basic Authentication.");
        }
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the rest of the pipeline is done.</returns>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            var credential = GetBasicCredential(context);

            if (!credential.CredentialsEntered)
            {
                credential = GetSecretCredential(context);
            }

            if (!credential.CredentialsEntered)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }
            var tokenCache = context.RequestServices.GetService<ITokenCache>();

            var cacheKey = BuildKey(credential);
            var tokenInfo = tokenCache?.Get<TokenInfo>(cacheKey);

            if (null == tokenInfo || tokenInfo.ExpiresOnUtc < DateTime.UtcNow.AddMinutes(1))
            {
                tokenInfo = await GetTokenFromCredentialAsync(credential, context.RequestServices).ConfigureAwait(false);

                tokenCache?.Put(cacheKey, tokenInfo);
            }

            if (tokenInfo is not null)
            {
                // Replace the Basic Authorization by the access token in the header.
                var authorization = new AuthenticationHeaderValue("Bearer", tokenInfo.Token).ToString();
                context.Request.Headers.Remove("Authorization");
                context.Request.Headers.Append("Authorization", authorization);
            }
            else
            {
                _logger.Technical().LogError($"No token has been created for the user {credential.Upn}.");
            }
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
        }

        await _next(context).ConfigureAwait(false);
    }

    private async Task<TokenInfo?> GetTokenFromCredentialAsync(CredentialsResult credential, IServiceProvider serviceProvider)
    {
        if (credential.CredentialsEntered)
        {
            var provider = serviceProvider.GetKeyedService<ITokenProvider>(_options.BasicSettings.Values[TokenKeys.ProviderIdKey]);

            if (null == provider)
            {
                _logger.Technical().LogError($"No token provider has been resolved with the ProviderId {_options.BasicSettings.Values[TokenKeys.ProviderIdKey]}.");
                return null;
            }

            // Get an Access Token. The credential extracted from the request is passed as the
            // platform parameter so the provider uses it instead of the settings User/Password.
            var result = await provider.GetTokenAsync(_options.BasicSettings, credential).ConfigureAwait(false);

            if (result.IsFailed)
            {
                _logger.Technical().LogError($"No token has been created for the user {credential.Upn}.");
                result.Log();
                return null;
            }

            return result.Value;
        }

        return null;
    }

    private static string BuildKey([DisallowNull] CredentialsResult credentialsResult)
    {
        ArgumentNullException.ThrowIfNull(credentialsResult);

        return "Basic-" + $"{credentialsResult.Upn}-{credentialsResult.Password}".GetHashCode().ToString(CultureInfo.InvariantCulture);
    }

    private CredentialsResult GetSecretCredential([DisallowNull] HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_options.CertificateHeaderOptions.Count == 0)
        {
            return new CredentialsResult(false);
        }

        foreach (var cert in _options.CertificateHeaderOptions)
        {
            var secret = GetClientSecretIfExist(context, cert.Key);

            // Decrypt the content!
            if (!string.IsNullOrWhiteSpace(secret))
            {
                var pair = cert.Value.Decrypt(secret);

                return new CredentialsResult(false).ExtractCredential(pair, _logger, _options.DefaultUpn);
            }
        }

        return new CredentialsResult(false);
    }

    private static string? GetClientSecretIfExist([DisallowNull] HttpContext context, [DisallowNull] string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(key);
        }

        var clientSecret = context.Request.Headers.FirstOrDefault(header => header.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)).Value.FirstOrDefault();

        // Check if this is in the url query string.
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            clientSecret = context.Request.Query.FirstOrDefault(p => p.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)).Value.FirstOrDefault();
        }

        return clientSecret;
    }

    private CredentialsResult GetBasicCredential([DisallowNull] HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authzValue) && authzValue.Any(value => value is not null && value.Contains("Basic")))
        {
            var authorization = authzValue.First(basic => basic is not null && basic.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase));

            if (null == authorization)
            {
                return new CredentialsResult(false);
            }
            // We have a Basic authentication.
            var token = authorization["Basic ".Length..].Trim();

            var pair = Encoding.UTF8.GetString(Convert.FromBase64String(token));

            return new CredentialsResult(false).ExtractCredential(pair, _logger, _options.DefaultUpn);
        }

        return new CredentialsResult(false);
    }
}
