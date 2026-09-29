using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Token;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>The source generated JSON serialization context used to read the token returned by the server.</summary>
[JsonSerializable(typeof(string))]
public partial class TokenJsonContext : JsonSerializerContext
{
}

/// <summary>
/// An <see cref="ITokenProvider"/> for a Blazor WebAssembly application whose token is given by its server backend. The token is requested with a GET on the
/// <c>TokenRequestUrl</c> of the settings, through the <see cref="HttpClient"/> named by <c>HttpClientName</c> (see <c>AddAuthenticationCookie</c>), and kept until it expires.
/// </summary>
/// <param name="httpClientFactory">The factory of the HTTP clients.</param>
/// <param name="logger">The logger.</param>
[Export(ProviderName, typeof(ITokenProvider)), Shared]
public class ClientTokenProvider(IHttpClientFactory httpClientFactory, ILogger<ClientTokenProvider> logger)
    : ITokenProvider
{
    /// <summary>The key (<c>Client</c>) under which the provider is registered.</summary>
    public const string ProviderName = "Client";

    private TokenInfo _token = new TokenInfo("bearer", string.Empty, DateTime.MinValue);

    /// <summary>Gets the token from the server backend, or the previous one when it is still valid.</summary>
    /// <param name="settings">The settings giving <c>HttpClientName</c> and <c>TokenRequestUrl</c>.</param>
    /// <param name="platformParameters">Not used.</param>
    /// <returns>The token, or a failed result when a setting is missing or the token cannot be obtained.</returns>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        if (_token.ExpiresOnUtc >= DateTime.UtcNow)
        {
            return Result.Ok(_token);
        }

        if (settings is null)
        {
            return Result.Fail("No settings provided.");
        }

        settings.Values.TryGetValue(TokenKeys.HttpClientName, out var httpClientName);
        if (string.IsNullOrWhiteSpace(httpClientName))
        {
            return Result.Fail("No HttpClientName provided.");
        }

        settings.Values.TryGetValue(TokenKeys.TokenRequestUrl, out var requestUrl);
        if (string.IsNullOrWhiteSpace(requestUrl))
        {
            return Result.Fail("No TokenRequestUrl provided.");
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            // Get the new token from the backend.
            var bearerToken = await httpClient.GetFromJsonAsync<string>(requestUrl, TokenJsonContext.Default.String)
                                                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(bearerToken))
            {
                return Result.Fail($"Unable to get a token, HttpClientFactory({httpClientName}), endpoint is {httpClient.BaseAddress?.ToString()}.");
            }

            _token = new TokenInfo("Bearer", bearerToken);
            return Result.Ok(_token);
        }
        catch (Exception e)
        {
            logger.Technical().LogException(e);
        }

        return Result.Fail("Unable to get the token.");
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
