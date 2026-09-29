using Arc4u.Dependency.Attribute;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Arc4u.Blazor.Handlers;

/// <summary>
/// Any request will include the cookie existing on the browser and will send this to the backend.
/// Backend must be in fact a Blazor SSR service.
/// </summary>
[Export]
public class AttachCookiesHandler : DelegatingHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var optionsKey = new HttpRequestOptionsKey<bool>(nameof(WebAssemblyHttpRequestMessageExtensions.SetBrowserRequestCredentials));

        if (!request.Options.TryGetValue(optionsKey, out _))
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        }
        return base.SendAsync(request, cancellationToken);
    }
}

