using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TokenProvider;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
sealed partial class ClientCredentialsJsonContext : JsonSerializerContext
{
}

/// <summary>
/// <c>grant_type=client_credentials</c> token provider authenticating the client with the
/// <c>client_secret_basic</c> method: the <c>ClientId</c>/<c>ClientSecret</c> pair is sent in the
/// HTTP <c>Authorization: Basic base64(client_id:client_secret)</c> header, while the scope and any
/// extra parameter (e.g. <c>resource</c>) travel in the form body.
/// <para>
/// It consumes the settings produced by <see cref="ClientCredentialsScenario"/> and caches the
/// acquired application token by client + scope + authority, refreshing it close to expiry.
/// </para>
/// </summary>
[Export(ProviderName, typeof(ITokenProvider)), Shared]
public class ClientCredentialsTokenProvider(
    IHttpClientFactory httpClientFactory,
    ITokenCache tokenCache,
    IOptionsMonitor<AuthorityOptions> authorities,
    ILogger<ClientCredentialsTokenProvider> logger) : ITokenProvider
{
    /// <summary>The key (<c>ClientCredentials</c>) under which the provider is registered.</summary>
    public const string ProviderName = "ClientCredentials";

    /// <summary>
    /// Gets an application token with the client credentials grant. The <c>ClientId</c>, <c>ClientSecret</c> and <c>Scope</c> settings are required; the authority is taken from
    /// the <c>Authority</c> setting (or the <c>Default</c> authority). A cached token is returned until it expires in less than one minute.
    /// </summary>
    /// <param name="settings">The provider settings (see <see cref="TokenKeys"/>).</param>
    /// <param name="_">Not used.</param>
    /// <returns>The token, or a failed result when a required setting is missing or the token cannot be obtained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? _)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var validation = new Result();

        if (!settings.Values.TryGetValue(TokenKeys.ClientIdKey, out var clientId) || string.IsNullOrWhiteSpace(clientId))
        {
            validation.WithError("ClientId is missing. Cannot process the request.");
        }

        if (!settings.Values.TryGetValue(TokenKeys.ClientSecret, out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret))
        {
            validation.WithError("ClientSecret is missing. Cannot process the request.");
        }

        if (!settings.Values.TryGetValue(TokenKeys.Scope, out var scope) || string.IsNullOrWhiteSpace(scope))
        {
            validation.WithError("Scope is missing. Cannot process the request.");
        }

        validation.LogIfFailed();
        if (validation.IsFailed)
        {
            return validation;
        }

        var authorityKey = settings.Values.TryGetValue(TokenKeys.AuthorityKey, out var key) ? key : "Default";
        var authority = authorities.Get(authorityKey);

        var extraParameters = settings.Values.TryGetValue(TokenKeys.ExtraParameters, out var extra)
            ? ExtraParametersEncoder.Decode(extra).ToList()
            : [];

        var cacheKey = BuildKey(authority, clientId!, scope!, clientSecret!);

        // Serve a still-valid token from the cache; refresh when missing or close to expiry.
        var cached = tokenCache.Get<TokenInfo>(cacheKey);
        if (cached is not null && cached.ExpiresOnUtc >= DateTime.UtcNow.AddMinutes(1))
        {
            logger.Technical().LogTokenLoadedFromCache(cacheKey);
            return Result.Ok(cached);
        }

        logger.Technical().LogCallSTS(cacheKey);
        var tokenEndpoint = await authority.GetEndpointAsync(CancellationToken.None).ConfigureAwait(false);
        var result = await GetTokenInfoAsync(tokenEndpoint, clientId!, clientSecret!, scope!, extraParameters).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            try
            {
                logger.Technical().LogSaveTokenInCache(cacheKey, result.Value.ExpiresOnUtc);
                tokenCache.Put(cacheKey, result.Value);
            }
            catch (Exception ex)
            {
                result.WithError(new ExceptionalError(ex));
            }
        }

        return result;
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

    private static string BuildKey(AuthorityOptions authority, string clientId, string scope, string clientSecret)
    {
        // The cache is distributed/persistent, so the key must be stable across processes and
        // restarts. Sort the extra parameters for order-independence and hash them with SHA-256
        // (string.GetHashCode is randomized per process and must not be used here).
        var extraHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authority.Url.AbsolutePath + scope + clientSecret)));
        return $"ClientCredentials_{clientId}_{extraHash}";
    }

    private async Task<Result<TokenInfo>> GetTokenInfoAsync(Uri tokenEndpoint, string clientId, string clientSecret, string scope, IEnumerable<KeyValuePair<string, string>> extraParameters)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();

            // client_secret_basic: the client credentials are sent as an HTTP Basic Authorization header.
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", scope }
            };
            // Some Idp require extra parameters (e.g. resource) to scope the token to the right Api.
            foreach (var extra in extraParameters)
            {
                parameters[extra.Key] = extra.Value;
            }

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await client.PostAsync(tokenEndpoint, content).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Failure(response.StatusCode.ToString(), responseBody);
            }

            var responseValues = JsonSerializer.Deserialize(responseBody, ClientCredentialsJsonContext.Default.DictionaryStringJsonElement)!;

            var accessToken = responseValues["access_token"].GetString()!;
            var offset = responseValues["expires_in"].GetInt64();
            var dateUtc = DateTime.UtcNow.AddSeconds(offset);

            return new TokenInfo("Bearer", accessToken, dateUtc);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return ValidationError.Create(ex.Message).WithCode("Rejected");
        }
    }

    private Result<TokenInfo> Failure(string statusCode, string responseBody)
    {
        // To avoid overflowing the log with a large response body, limit its length.
        var loggedResponseBody = responseBody;
        const int MaxResponseBodyLength = 256;
        if (loggedResponseBody is not null && loggedResponseBody.Length > MaxResponseBodyLength)
        {
            loggedResponseBody = $"{responseBody[..MaxResponseBodyLength]}...(response truncated, {loggedResponseBody.Length} total characters)";
        }

        // In case of error, any extra information should be Json with string values, but we can't assume it always is.
        Dictionary<string, string>? dictionary = null;
        try
        {
            dictionary = JsonSerializer.Deserialize(responseBody, ClientCredentialsJsonContext.Default.DictionaryStringString);
        }
        catch
        {
            // the response body was not Json (it happens)
        }

        if (dictionary is null)
        {
            logger.Technical().LogClientCredentialsTokenError(statusCode, loggedResponseBody ?? string.Empty);
            return ValidationError.Create($"{statusCode} occurred while requesting a client_credentials token.").WithCode("TokenError");
        }

        var technical = logger.Technical();
        foreach (var kv in dictionary)
        {
            technical.Add(kv.Key, kv.Value);
        }
        technical.LogClientCredentialsTokenError(statusCode, loggedResponseBody ?? string.Empty);

        if (dictionary.TryGetValue("error", out var tokenErrorCode))
        {
            if (!dictionary.TryGetValue("error_description", out var error_description))
            {
                error_description = "No error description";
            }
            return ValidationError.Create(error_description).WithCode(tokenErrorCode ?? string.Empty);
        }

        return ValidationError.Create($"{statusCode} occurred while requesting a client_credentials token.").WithCode("TokenError");
    }
}
