using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Middleware;

/// <summary>
/// A middleware that gives a bearer token to requests authenticated with the OpenID Connect (cookie) authentication, so the token can be used by the code that reads the <c>Authorization</c> header.
/// The token is requested from the on-behalf-of provider when on-behalf-of settings exist, otherwise from the provider of the OpenID settings. The header is replaced by <c>Bearer &lt;token&gt;</c>
/// and the token is stored in the <see cref="ClaimsIdentity.BootstrapContext"/>. The culture of the principal profile is added to the request in the <c>culture</c> header.
/// </summary>
public class OpenIdBearerInjectorMiddleware
{
    private readonly OpenIdBearerInjectorSettingsOptions _options;
    private readonly RequestDelegate _next;
    private ActivitySource? _activitySource;

    /// <summary>Initializes a new instance of the <see cref="OpenIdBearerInjectorMiddleware"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="options">The settings of the injector.</param>
    /// <exception cref="ConfigurationException">The options have no current value.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public OpenIdBearerInjectorMiddleware([DisallowNull] RequestDelegate next, [DisallowNull] IOptionsMonitor<OpenIdBearerInjectorSettingsOptions> options)
    {
        _options = options.CurrentValue ?? throw new ConfigurationException("No current value exists for OpenIdBearerInjectorSettingsOptions");
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="activitySourceFactory">The factory of the activity source used for telemetry.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that completes when the rest of the pipeline is done.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task InvokeAsync([DisallowNull] HttpContext context, IActivitySourceFactory activitySourceFactory, ILogger<OpenIdBearerInjectorMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity is not null
            && context.User.Identity.IsAuthenticated
            && context.User.Identity.AuthenticationType!.Equals(_options.OpenIdSettings.Values[TokenKeys.AuthenticationTypeKey], StringComparison.InvariantCultureIgnoreCase))
        {
            if (context.User is AppPrincipal principal)
            {
                if (null != principal?.Profile?.CurrentCulture)
                {
                    context.Request?.Headers?.Append("culture", principal.Profile.CurrentCulture.TwoLetterISOLanguageName);
                }
            }

            _activitySource ??= activitySourceFactory.GetArc4u();

            using var activity = _activitySource?.StartActivity("Inject bearer token in header", ActivityKind.Producer);
            Result<TokenInfo> tokenInfoResult = new();

            if (_options.OnBehalfOfOpenIdSettings is not null && _options.OnBehalfOfOpenIdSettings.Values.Any())
            {
                try
                {
                    var provider = context.RequestServices.GetKeyedService<ITokenProvider>(_options.OboProviderKey);

                    if (provider is null)
                    {
                        tokenInfoResult.WithError($"The token provider {_options.OboProviderKey} is not found!");
                    }

                    if (tokenInfoResult.IsSuccess)
                    {
                        tokenInfoResult = await provider!.GetTokenAsync(_options.OnBehalfOfOpenIdSettings, null).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    tokenInfoResult = new ExceptionalError(ex);
                }

            }
            else
            {
                try
                {
                    var provider = context.RequestServices.GetKeyedService<ITokenProvider>(_options.OpenIdSettings.Values[TokenKeys.ProviderIdKey]);

                    if (provider is null)
                    {
                        tokenInfoResult.WithError($"The token provider {_options.OpenIdSettings.Values[TokenKeys.ProviderIdKey]} is not found!");
                    }

                    if (tokenInfoResult.IsSuccess)
                    {
                        tokenInfoResult = await provider!.GetTokenAsync(_options.OpenIdSettings, null).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    tokenInfoResult = new ExceptionalError(ex);
                }
            }

            if (tokenInfoResult.IsSuccess)
            {
                var authorization = new AuthenticationHeaderValue("Bearer", tokenInfoResult.Value.Token).ToString();
                context.Request!.Headers.Remove("Authorization");
                context.Request.Headers.Append("Authorization", authorization);

                // Set the current identity BoostrapContext with the value of the token!
                if (context.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    claimsIdentity.BootstrapContext = tokenInfoResult.Value.Token;
                }

            }

            tokenInfoResult.LogIfFailed();
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }
}

