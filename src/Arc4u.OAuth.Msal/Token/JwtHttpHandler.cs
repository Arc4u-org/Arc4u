using System.Net.Http.Headers;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Msal.Token;

public class JwtHttpHandler : DelegatingHandler
{
    // on the Frontend, we have to retrieve the user context based the singleton instance of the application context
    // service if this was done in the context of a user (via rest api or gRPC service).

    /// <summary>
    /// This is a ctor to use only in a frontend scenario.
    /// No inner handler is defined because this will be done via the AddHttpClient method in a service!
    /// </summary>
    /// <param name="container">The scoped container</param>
    /// <param name="logger"></param>
    /// <param name="resolvingName">The name used to resolve the settings</param>
    public JwtHttpHandler(IServiceProvider container, ILogger<JwtHttpHandler> logger, string resolvingName)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));

        _logger = logger;

        if (!container.TryGetNamedSettings(resolvingName, out _settings))
        {
            _logger.LogResolvingIssueSettingsByName(resolvingName);
        }

        container.TryGetService(out _applicationContext);
    }

    public JwtHttpHandler(IServiceProvider container, ILogger<JwtHttpHandler> logger, IKeyValueSettings settings)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _logger = logger;

        container.TryGetService(out _applicationContext);
    }

    private readonly IKeyValueSettings? _settings;
    private readonly IServiceProvider _container;
    private readonly IApplicationContext? _applicationContext;
    private readonly ILogger<JwtHttpHandler> _logger;

    private IServiceProvider GetResolver() => _container;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.Technical().LogHttpHandlerIsCalled(GetType().Name);

        if (null == _settings || null == _applicationContext)
        {
            _logger.Technical().LogCallNextHttpHandler(GetType().Name);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Or we have an OAuth token and we have to validate if the Authenication Type is 
        // well the same as the ClaimsPrincipal!
        // Or we inject in a header another kind of token and we just inject it (no other check).
        // By pass is provided with the AuthenticationType value = "Inject"

        if (!_settings.Values.TryGetValue(TokenKeys.AuthenticationTypeKey, out var authenticationType))
        {
            _logger.Technical().LogNoAuthenticationTypeCallNextHttpHandler(GetType().Name);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var inject = authenticationType.Equals("inject", StringComparison.InvariantCultureIgnoreCase);

        // if we don't inject a bearer token, the AuthenticationType defined in the settings must be the same as the authentication type defined in the Identity.
        if (!inject
            && null != _applicationContext?.Principal?.Identity
            && null != _applicationContext?.Principal?.Identity.AuthenticationType
            && !authenticationType.ToLowerInvariant().Contains(_applicationContext.Principal.Identity.AuthenticationType.ToLowerInvariant()))
        {
            _logger.Technical().LogDifferentAuthenticationTypeCallNextHttpHandler(GetType().Name);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // if we inject more than one bearer token do it only if no one exist already.
        if (null != request.Headers.Authorization)
        {
            _logger.Technical().LogAlreadyHasAnAuthorizationHeaderCallNextHttpHandler(GetType().Name);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _logger.Technical().LogGetTheTokenProvider(GetType().Name);

        var provider = GetResolver().GetKeyedService<ITokenProvider>(_settings.Values[TokenKeys.ProviderIdKey]);

        if (null == provider)
        {
            _logger.Technical().LogNoTokenProviderDefined(_settings.Values[TokenKeys.ProviderIdKey]);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _logger.Technical().LogRequestingAuthenticationToken();
        var tokenInfoResult = await provider.GetTokenAsync(_settings, null).ConfigureAwait(false);

        // check if the token is still valid.
        // This is due to gRPC. It is possible that a gRPC streaming call is not closed and the token in the HttpContext is expired.
        // It is also possible this with OAuth where the token is added to the Identity and used like this => no refresh of the token is possible.
        if (tokenInfoResult.IsFailed || (tokenInfoResult.IsSuccess && tokenInfoResult.Value.ExpiresOnUtc < DateTime.UtcNow))
        {
            _logger.Technical().LogExpiredToken();
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _logger.Technical().LogRemoveBearerToken();
        request.Headers.Remove("Bearer");

        var tokenInfo = tokenInfoResult.Value;

        var scheme = inject ? tokenInfo.TokenType : "Bearer";
        _logger.Technical().LogAddSchemeToToken(scheme);

        if (new string[] { "Bearer", "Basic" }.Any(s => s.Equals(scheme, StringComparison.InvariantCultureIgnoreCase)))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, tokenInfo.Token);
        }
        else
        {
            request.Headers.Add(scheme, tokenInfo.Token);
        }

        // Add ActivityId if founded!
        if (null != _applicationContext?.Principal)
        {
            if (null != _applicationContext?.ActivityID)
            {
                _logger.Technical().LogAddActivityId(_applicationContext.ActivityID);
                request.Headers.Add("activityid", _applicationContext.ActivityID);
            }

            _logger.Technical().LogAddCulture(_applicationContext!.Principal.Profile?.CurrentCulture?.TwoLetterISOLanguageName ?? "No Culture exists.");
            var culture = _applicationContext.Principal.Profile?.CurrentCulture?.TwoLetterISOLanguageName;
            if (null != culture)
            {
                request.Headers.Add("culture", culture);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
