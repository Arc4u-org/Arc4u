using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.Blazor.Handlers;

/// <summary>
/// A <see cref="DelegatingHandler"/> that adds the token of the current user to the requests of an <see cref="HttpClient"/> of a Blazor application.
/// The token is requested from the <see cref="ITokenProvider"/> designated by the <c>ProviderId</c> key of the settings and sent in the <c>Authorization</c> header. Errors are logged and the request is sent without token.
/// </summary>
/// <param name="container">The service provider used to resolve the keyed token provider.</param>
/// <param name="logger">The logger.</param>
/// <param name="settings">The settings designating the token provider.</param>
public class JwtHttpHandler(IServiceProvider container, ILogger<JwtHttpHandler> logger, SimpleKeyValueSettings settings) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || settings.Values.Count == 0)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (settings.Values.TryGetValue(TokenKeys.ProviderIdKey, out var providerId))
            {
                var tokenProvider = container.GetRequiredKeyedService<ITokenProvider>(providerId);
                var tokenResult = await tokenProvider.GetTokenAsync(settings, null).ConfigureAwait(false);

                if (tokenResult.IsSuccess)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        tokenResult.Value.TokenType,
                        tokenResult.Value.Token);
                }

                tokenResult.LogIfFailed();
            }
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
