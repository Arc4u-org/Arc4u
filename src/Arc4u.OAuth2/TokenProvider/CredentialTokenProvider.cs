using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TokenProvider;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]

sealed partial class CredentialTokenJsonContext : JsonSerializerContext
{
}

/// <summary>
/// An <see cref="ICredentialTokenProvider"/> that requests a token from the token endpoint of an authority with the OAuth2 resource owner password grant.
/// The provider is registered under the name <see cref="ProviderName"/>. The authority is read from the <c>Authority</c> key of the settings (or the <c>Default</c> authority), the
/// <c>ClientId</c>, optional <c>ClientSecret</c> and <c>Scope</c> (default <c>openid</c>) keys come from the settings too. Failing requests are retried during <see cref="AuthorityOptions.RetryInterval"/> (90 seconds by default).
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="authorityOptions">The named authority options.</param>
[Export(ProviderName, typeof(ICredentialTokenProvider)), Shared]
public class CredentialTokenProvider(ILogger<CredentialTokenProvider> logger, IOptionsMonitor<AuthorityOptions> authorityOptions) : ICredentialTokenProvider
{
    /// <summary>The name (<c>CredentialDirect</c>) under which the provider is registered.</summary>
    public const string ProviderName = "CredentialDirect";

    private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(90);

    private readonly ILogger<CredentialTokenProvider> _logger = logger;

    /// <inheritdoc/>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings settings, CredentialsResult credential)
    {
        var result = GetContext(settings, out var clientId, out var authority, out var scope, out var clientSecret);

        if (null == authority)
        {
            throw new NullReferenceException(nameof(authority));
        }
        var tokenEndpoint = await authority.GetEndpointAsync(CancellationToken.None).ConfigureAwait(false);

        _logger.Technical().Add("ClientId", clientId)
                           .Add("Scope", scope)
                           .Add("Authority", tokenEndpoint)
                           .LogGetEndpoint();

        if (string.IsNullOrWhiteSpace(credential.Upn))
        {
            result.WithValidationError("No Username is provided.");
        }

        if (string.IsNullOrWhiteSpace(credential.Password))
        {
            result.WithValidationError("No password is provided.");
        }

        result.LogIfFailed();
        if (result.IsFailed)
        {
            return result;
        }

        // no cache, do a direct call on every calls.
        _logger.Technical().LogStsAndUser(authority.Url.ToString(), credential.Upn!);
        return await GetTokenInfoAsync(clientSecret, clientId, tokenEndpoint, scope, credential.Upn!, credential.Password!, authority.RetryInterval ?? DefaultRetryInterval).ConfigureAwait(false);

    }

    private Result GetContext(IKeyValueSettings settings, out string clientId, out AuthorityOptions? authority, out string scope, out string clientSecret)
    {
        // Check the information.
        if (null == settings)
        {
            clientId = string.Empty;
            authority = null;
            scope = string.Empty;
            clientSecret = string.Empty;

            return ValidationError.Create("Settings parameter cannot be null.");
        }

        // Valdate arguments.
        if (!settings.Values.ContainsKey(TokenKeys.AuthorityKey))
        {
            authority = authorityOptions.Get("Default");
        }
        else
        {
            authority = authorityOptions.Get(settings.Values[TokenKeys.AuthorityKey]);
        }

        if (!settings.Values.ContainsKey(TokenKeys.ClientIdKey))
        {
            clientId = string.Empty;
            authority = null;
            scope = string.Empty;
            clientSecret = string.Empty;

            return ValidationError.Create("ClientId is missing. Cannot process the request.");
        }

        _logger.Technical().LogCreatingAuthenticationContext();
        clientId = settings.Values[TokenKeys.ClientIdKey];
        clientSecret = settings.Values.ContainsKey(TokenKeys.ClientSecret) ? settings.Values[TokenKeys.ClientSecret] : string.Empty;
        // More for backward compatibility! We should throw an error message if scope is not defined...
        scope = !settings.Values.ContainsKey(TokenKeys.Scope) ? Constants.OpenIdScope : settings.Values[TokenKeys.Scope];
        return Result.Ok();
    }

    /// <summary>
    /// If a request could not be made, we need to track the number of retries and the delay between them.
    /// This is used in the logs and in the exception message, to allow for better diagnostics.
    /// </summary>
    private sealed class RetryInformation
    {
        public int RetryCount;
        public TimeSpan Delay;

        public override string ToString()
        {
            return RetryCount == 0 ? "No retries" : $"Retried {RetryCount} times over {Delay}";
        }
    }

    /// <summary>
    /// We do this without Polly since this will need to be integrated in Arc4u at some point.
    /// </summary>
    private sealed class HttpRetryMessageHandler(HttpMessageHandler innerHandler, TimeSpan retryInterval, CredentialTokenProvider.RetryInformation retryInformation) : DelegatingHandler(innerHandler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            retryInformation.RetryCount = 0;
            var sw = Stopwatch.StartNew();
            var random = new Random();
            for (; ; )
            {
                HttpResponseMessage? response = null;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(4, random.Next(1, 6)));
                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    // we always return the response if it is successful or we have reached the retry timespan.
                    if (response.IsSuccessStatusCode || sw.Elapsed >= retryInterval)
                    {
                        sw.Stop();
                        retryInformation.Delay = sw.Elapsed;
                        return response;
                    }

                    // Use "Retry-After" value, if available. Typically, this is sent with either a 503 (Service Unavailable) or 429 (Too Many Requests):
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After
                    if (response.Headers.RetryAfter is not null)
                    {
                        if (response.Headers.RetryAfter.Date.HasValue)
                        {
                            delay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                        }
                        else if (response.Headers.RetryAfter.Delta.HasValue)
                        {
                            delay = response.Headers.RetryAfter.Delta.Value;
                        }
                    }

                    response.Dispose();
                }
                catch when (sw.Elapsed < retryInterval)
                {
                    // Ignore the exception if we have retries left. But we need to dispose the response even though it's most likely null.
                    response?.Dispose();
                }
                catch
                {
                    sw.Stop();
                    retryInformation.Delay = sw.Elapsed;
                }
                ++retryInformation.RetryCount;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<Result<TokenInfo>> GetTokenInfoAsync(string? clientSecret, string clientId, Uri tokenEndpoint, string scope, string upn, string pwd, TimeSpan retryInterval)
    {
        var retryInformation = new RetryInformation();

        using var handler = new HttpClientHandler { UseDefaultCredentials = true };
        using var client = new HttpClient(new HttpRetryMessageHandler(handler, retryInterval, retryInformation));

        try
        {
            var parameters = new Dictionary<string, string>
                    {
                        { "client_id", clientId },
                        { "grant_type", "password" },
                        { "username", upn.Trim() },
                        { "password", pwd.Trim() },
                        { "scope", scope }
                    };
            if (!string.IsNullOrWhiteSpace(clientSecret))
            {
                parameters.Add("client_secret", clientSecret!);
            }
            using var content = new FormUrlEncodedContent(parameters);

            using var response = await client.PostAsync(tokenEndpoint, content).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // We model this after https://www.rfc-editor.org/rfc/rfc6749#section-5.2
            // Identity providers usually reply with wither HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized, but in practice they can also reply with other
            // status codes that signal failure. We want to write as much information as possible in the logs in any case, but throw exceptions with minimal information for security.
            if (!response.IsSuccessStatusCode)
            {
                // To avoid overflowing the log with a large response body, we make sure that we limit its length. This should be a rare occurrence.
                var loggedResponseBody = responseBody;
                const int MaxResponseBodyLength = 256;  // arbitrary
                if (loggedResponseBody != null && loggedResponseBody.Length > MaxResponseBodyLength)
                {
                    loggedResponseBody = $"{responseBody[..MaxResponseBodyLength]}...(response truncated, {loggedResponseBody.Length} total characters)";
                }

                var logger = _logger.Technical();

                // In case of error, any extra information should be in Json with string values, but we can't assume this is always the case!
                Dictionary<string, string>? dictionary = null;
                try
                {
                    dictionary = JsonSerializer.Deserialize(responseBody, CredentialTokenJsonContext.Default.DictionaryStringString);
                }
                catch
                {
                    // the response body was not Json (it happens)
                }
                // we cannot any any more meaningful information to the log if this is not a dictionary
                if (dictionary == null)
                {
                    logger.LogCredentialToken(upn, response.StatusCode.ToString(), loggedResponseBody); ;
                }
                else
                {
                    // add the key/values are properties of the structured log
                    foreach (var kv in dictionary)
                    {
                        logger.Add(kv.Key, kv.Value);
                    }
                    logger.LogCredentialToken(upn, response.StatusCode.ToString(), loggedResponseBody);

                    if (dictionary.TryGetValue("error", out var tokenErrorCode))
                    {
                        // error description is optional. So is error_uri, but we don't use it.
                        if (!dictionary.TryGetValue("error_description", out var error_description))
                        {
                            error_description = "No error description";
                        }

                        return ValidationError.Create($"{error_description} ({upn})").WithCode(tokenErrorCode ?? string.Empty);
                    }
                }
                // if we can't write a better exception, issue a more general one
                return ValidationError.Create($"{response.StatusCode} occured while requesting a token for {upn}").WithCode("TokenError");
            }

            // at this point, we *must* have a valid Json response. The values are a mixture of strings and numbers, so we deserialize the JsonElements
            var responseValues = JsonSerializer.Deserialize(responseBody, CredentialTokenJsonContext.Default.DictionaryStringJsonElement)!;

            _logger.Technical().LogTokenReceived(upn);

            var accessToken = responseValues["access_token"].GetString()!;
            var tokenType = "Bearer"; //  responseValues["token_type"]; Issue on Adfs return bearer and not Bearer (ok in AzureAD).
                                      // expires in is in ms.
            var offset = responseValues["expires_in"].GetInt64();

            // expiration lifetime in is in seconds.
            var dateUtc = DateTime.UtcNow.AddSeconds(offset);

            _logger.Technical().LogTokenExpiration(dateUtc);

            return new TokenInfo(tokenType, accessToken, dateUtc);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return ValidationError.Create(ex.Message).WithCode("Rejected");
        }
    }
}
