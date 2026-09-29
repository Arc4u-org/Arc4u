using System.Security.Claims;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.gRPC.Interceptors;

/// <summary>
/// Inject in the Metadata's message the Bearer token of the authenticated user.
/// </summary>
public class OAuth2Interceptor<T> : Interceptor
{
    /// <summary>
    /// This is the constructor to use in a Client scenario like a Wpf or a MAUI or a console.
    /// The <see cref="IApplicationContext"/> and the <see cref="ITokenProvider"/> are not scoped to an httpRequest or a job, etc...
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/></param>
    /// <param name="logger"><see cref="ILogger"/></param>
    /// <param name="keyValuesSettings">Property bag for the token povider.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public OAuth2Interceptor(IServiceProvider serviceProvider, ILogger<T> logger, IKeyValueSettings keyValuesSettings)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        serviceProvider.TryGetService(out _serviceProviderAccessor);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _settings = keyValuesSettings ?? throw new ArgumentNullException(nameof(keyValuesSettings));
    }

    private readonly IKeyValueSettings _settings;
    private readonly ILogger<T> _logger;
    private readonly IScopedServiceProviderAccessor? _serviceProviderAccessor;
    private readonly IServiceProvider? _serviceProvider;
    private static readonly string[] SourceArray = ["Bearer", "Basic"];

    private IServiceProvider GetResolver()
    {
        var serviceProvider = _serviceProviderAccessor?.ServiceProvider;

        if (serviceProvider is null)
        {
            return _serviceProvider ?? throw new InvalidOperationException("The service provider is not defined. Use the other constructor");
        }

        return serviceProvider;
    }
    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        AddBearerTokenCallerMetadata(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        AddBearerTokenCallerMetadata(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        AddBearerTokenCallerMetadata(ref context);

        return continuation(context);
    }

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        AddBearerTokenCallerMetadata(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        AddBearerTokenCallerMetadata(ref context);

        return continuation(context);
    }
    private void AddBearerTokenCallerMetadata<TRequest, TResponse>(ref ClientInterceptorContext<TRequest, TResponse> context)
                where TRequest : class
                where TResponse : class
    {
        var headers = context.Options.Headers;

        // Call doesn't have a headers collection to add to.
        // Need to create a new context with headers for the call.
        if (headers == null)
        {
            headers = [];
            var options = context.Options.WithHeaders(headers);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        }

        // if we have already an "Authorization" defined, we can skip the code here.
        if (null != headers.GetValue("authorization"))
        {
            _logger.Technical().LogSkipAddingBearerToken(_settings.Values[TokenKeys.AuthenticationTypeKey]);
            return;
        }

        var applicationContext = GetCallContext(out var containerResolve);

        if (applicationContext is null || containerResolve is null)
        {
            _logger.Technical().LogNoApplicationContextIsDefined(GetType().Name);
            return;
        }

        if (!_settings.Values.TryGetValue(TokenKeys.AuthenticationTypeKey, out var authenticationType))
        {
            _logger.Technical().LogNoAuthenticationTypeIsDefined(GetType().Name);
            return;
        }

        var inject = authenticationType.Equals("inject", StringComparison.InvariantCultureIgnoreCase);

        if (null == applicationContext.Principal)
        {
            _logger.Technical().LogNoUserContext();
            return;
        }

        var claimsIdentity = applicationContext.Principal?.Identity as ClaimsIdentity;
        // if we don't inject a bearer token, the AuthenticationType defined in the settings must be the same as the authentication type defined in the Identity.
        if (!inject
            &&
            claimsIdentity is not null
            &&
            claimsIdentity.AuthenticationType != null
            &&
            !claimsIdentity.AuthenticationType.Equals(_settings.Values[TokenKeys.AuthenticationTypeKey], StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        // But in case we inject we need something in the identity!
        if (claimsIdentity is null && !inject)
        {
            return;
        }

        try
        {
            var provider = containerResolve.GetKeyedService<ITokenProvider>(_settings.Values[TokenKeys.ProviderIdKey]);

            if (provider is null)
            {
                _logger.Technical().LogNoTokenProviderIsDefined(GetType().Name);
                return;
            }

            var tokenInfoResult = provider.GetTokenAsync(_settings, claimsIdentity).Result;

            if (tokenInfoResult.IsFailed)
            {
                _logger.Technical().LogNoTokenIsProvided(GetType().Name);
                tokenInfoResult.Log();
                return;
            }

            var tokenInfo = tokenInfoResult.Value;
            if (tokenInfo.ExpiresOnUtc < DateTime.UtcNow)
            {
                _logger.Technical().LogGrpcTokenIsExpired();
                return;
            }

            var scheme = inject ? tokenInfo.TokenType : "Bearer";
            _logger.Technical().LogAddSchemeToken(scheme);

            if (SourceArray.Any(s => s.Equals(scheme, StringComparison.InvariantCultureIgnoreCase)))
            {
                headers.Add("authorization", $"{scheme} {tokenInfo.Token}");
            }
            else
            {
                headers.Add(scheme, tokenInfo.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
        }

        // Add culture and activityID if exists!
        var culture = applicationContext.Principal?.Profile.CurrentCulture.TwoLetterISOLanguageName;
        if (culture is null || null != headers.GetValue("culture"))
        {
            return;
        }

        _logger.Technical().LogAddCurrentCulture(applicationContext.Principal?.Profile.CurrentCulture.TwoLetterISOLanguageName ?? "No Culture found");
        headers.Add("culture", culture);
    }

    private IApplicationContext? GetCallContext(out IServiceProvider? containerResolve)
    {
        containerResolve = GetResolver();

        return containerResolve?.GetService<IApplicationContext>();
    }
}
