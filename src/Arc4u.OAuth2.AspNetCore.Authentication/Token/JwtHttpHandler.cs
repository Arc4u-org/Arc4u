using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Extensions;
using Arc4u.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that adds the token of the current user to the requests of an <see cref="HttpClient"/>.
    /// The token provider is selected by the <c>ProviderId</c> of the settings. Nothing is added when the request already has an <c>Authorization</c> header,
    /// when the authentication type of the settings differs from the one of the current identity (unless it is <c>Inject</c>), when no provider or no valid token is found.
    /// The culture of the user is added in a <c>culture</c> header.
    /// </summary>
    /// <typeparam name="T">The type used as category of the logger.</typeparam>
    public class JwtHttpHandler<T> : DelegatingHandler
    {

        // on the backend, we have to retrieve the user context based on his scoped context when we do a request to another
        // service if this was done in the context of a user (via rest api or gRPC service).
        // When we do a call from a service account, in this case the user is fixed and not scoped => so the creation of the user in this
        // case can be a singleton because we do an impersonation!

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtHttpHandler{T}"/> class. To use in a backend scenario, where no platform parameters are used.
        /// No inner handler is defined because this will be done via the AddHttpClient method in a service!
        /// </summary>
        /// <param name="serviceProvider">The service provider, used when no scoped service provider is available (for example outside of a request).</param>
        /// <param name="logger">The logger.</param>
        /// <param name="keyValuesSettings">The settings selecting the token provider (<c>ProviderId</c>) and the authentication type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keyValuesSettings"/> is <see langword="null"/>.</exception>
        public JwtHttpHandler(IServiceProvider serviceProvider, ILogger<T> logger, [DisallowNull] IKeyValueSettings keyValuesSettings)
        {
            _serviceProvider = serviceProvider;
            _serviceProviderAccessor = serviceProvider.GetRequiredService<IScopedServiceProviderAccessor>();

            _logger = logger;

            _settings = keyValuesSettings ?? throw new ArgumentNullException(nameof(keyValuesSettings));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtHttpHandler{T}"/> class with the settings registered by name. To use in a backend scenario, where no platform parameters are used.
        /// No inner handler is defined because this will be done via the AddHttpClient method in a service!
        /// </summary>
        /// <param name="serviceProvider">The service provider, used to resolve the settings and when no scoped service provider is available (for example outside of a request).</param>
        /// <param name="logger">The logger.</param>
        /// <param name="resolvingName">The name of the <see cref="SimpleKeyValueSettings"/> options selecting the token provider (<c>ProviderId</c>) and the authentication type.</param>
        /// <exception cref="ConfigurationException">No settings exist with that name.</exception>
        public JwtHttpHandler(IServiceProvider serviceProvider, ILogger<T> logger, string resolvingName)
            : this(serviceProvider, logger, ResolveSettings(serviceProvider, logger, resolvingName))
        {
        }

        private static IKeyValueSettings ResolveSettings(IServiceProvider serviceProvider, ILogger<T> logger, string resolvingName)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(logger);

            if (serviceProvider.TryGetNamedSettings(resolvingName, out var settings))
            {
                return settings;
            }

            logger.Technical().LogResolvingIssueSettingsByName(resolvingName);
            throw new ConfigurationException($"No settings found for {resolvingName}.");
        }

        private readonly IKeyValueSettings? _settings;
        private readonly IServiceProvider? _serviceProvider;
        private readonly IScopedServiceProviderAccessor _serviceProviderAccessor;
        private readonly ILogger<T> _logger;

        private IServiceProvider? GetResolver()
        {
            IServiceProvider serviceProvider;

            try
            {
                serviceProvider = _serviceProviderAccessor.ServiceProvider;
            }
            catch (NullReferenceException)
            {
                if (_serviceProvider is null)
                {
                    throw new InvalidOperationException("The service provider is not defined. Use the other constructor");
                }
                serviceProvider = _serviceProvider;
            }
            return serviceProvider;
        }

        private IApplicationContext? GetCallContext(out IServiceProvider? containerResolve)
        {
            containerResolve = GetResolver();

            return containerResolve?.GetService<IApplicationContext>();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.Technical().LogHttpHandlerIsCalled(GetType().Name);

            var applicationContext = GetCallContext(out var containerResolve);

            if (_settings is null || applicationContext is null || containerResolve is null)
            {
                _logger.Technical().LogResolvingIssueSettingsByName(GetType().Name);
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

            var inject = authenticationType.Equals(Constants.InjectAuthenticationType, StringComparison.OrdinalIgnoreCase);

            // if we don't inject a bearer token, the AuthenticationType defined in the settings must be the same as the authentication type defined in the Identity.
            if (!inject
                && applicationContext?.Principal?.Identity?.AuthenticationType is not null
                && !authenticationType.Contains(applicationContext.Principal.Identity.AuthenticationType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Technical().LogDifferentAuthenticationTypeCallNextHttpHandler(GetType().Name);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            // if we inject more than one bearer token do it only if no one exist already.
            if (request.Headers.Authorization is not null)
            {
                _logger.Technical().LogAlreadyHasAnAuthorizationHeaderCallNextHttpHandler(GetType().Name);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            _logger.Technical().LogGetTheTokenProvider(GetType().Name);

            var provider = containerResolve.GetKeyedService<ITokenProvider>(_settings.Values[TokenKeys.ProviderIdKey]);
            if (provider is null)
            {
                _logger.Technical().LogNoTokenProvider(GetType().Name);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            _logger.Technical().LogRequestingToken();
            var tokenInfoResult = await provider.GetTokenAsync(_settings, null).ConfigureAwait(false);

            if (tokenInfoResult.IsFailed)
            {
                _logger.Technical().LogNoTokenProvider(GetType().Name);
                tokenInfoResult.Log();
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            // check if the token is still valid.
            // This is due to gRPC. It is possible that a gRPC streaming call is not closed and the token in the HttpContext is expired.
            // It is also possible this with OAuth where the token is added to the Identity and used like this => no refresh of the token is possible.
            if (tokenInfoResult.Value.ExpiresOnUtc < DateTime.UtcNow)
            {
                _logger.Technical().LogTokenIsExpired();
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            _logger.Technical().LogRemoveAnyBearer();
            request.Headers.Remove("Bearer");

            var scheme = inject ? tokenInfoResult.Value.TokenType : "Bearer";
            _logger.Technical().LogAddSchentoToken(scheme);

            if (_supportedSchemes.Any(s => s.Equals(scheme, StringComparison.OrdinalIgnoreCase)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(scheme, tokenInfoResult.Value.Token);
            }
            else
            {
                request.Headers.Add(scheme, tokenInfoResult.Value.Token);
            }

            // Add the culture of the user if any.
            if (applicationContext?.Principal is not null)
            {
                var culture = applicationContext!.Principal?.Profile?.CurrentCulture?.TwoLetterISOLanguageName;
                if (culture is not null)
                {
                    request.Headers.Add("culture", culture);
                    _logger.Technical().LogUseCurrentCulture(culture);

                }
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static readonly string[] _supportedSchemes = new[] { "Bearer", "Basic" };
    }
}
