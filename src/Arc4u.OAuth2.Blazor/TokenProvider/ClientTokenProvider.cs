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
    : ISignOutTokenProvider
{
    /// <summary>The key (<c>Client</c>) under which the provider is registered.</summary>
    public const string ProviderName = "Client";

    private static readonly TokenInfo NoToken = new("bearer", string.Empty, DateTime.MinValue);

    private TokenInfo _token = NoToken;

    // Incremented by each sign-out, so a token requested before a sign-out is not kept after it.
    private readonly SignOutGeneration _signOutGeneration = new();

    /// <summary>Gets the token from the server backend, or the previous one when it is still valid.</summary>
    /// <param name="settings">The settings giving <c>HttpClientName</c> and <c>TokenRequestUrl</c>.</param>
    /// <param name="platformParameters">Not used.</param>
    /// <returns>The token, or a failed result when a setting is missing or the token cannot be obtained.</returns>
    public async ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        // Like the other providers, a token expiring in less than one minute is renewed, so it does not expire while the request is in flight.
        // The token is read once: a sign-out can replace it between a check and a second read.
        var token = _token;
        if (token.ExpiresOnUtc > DateTime.UtcNow.AddMinutes(1))
        {
            return Result.Ok(token);
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

        var signOutCount = _signOutGeneration.Snapshot();

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

            var received = new TokenInfo("Bearer", bearerToken);

            // The user signed out while the token was requested: the token belongs to the previous session and is not kept.
            if (_signOutGeneration.HasChanged(signOutCount))
            {
                return Result.Fail("The user signed out while the token was requested.");
            }

            _token = received;
            return Result.Ok(received);
        }
        catch (Exception e)
        {
            logger.Technical().LogException(e);
        }

        return Result.Fail("Unable to get the token.");
    }

    /// <summary>
    /// Forgets the token kept in memory, so the next call asks the server backend again.
    /// Signing out of the backend (its authentication cookie) is done by the application.
    /// </summary>
    /// <param name="settings">The provider settings, which are not used.</param>
    /// <param name="cancellationToken">A token to cancel the operation, which is not used.</param>
    /// <returns>A completed <see cref="ValueTask"/>.</returns>
    public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        _signOutGeneration.Increment();
        _token = NoToken;

        return ValueTask.CompletedTask;
    }
}
