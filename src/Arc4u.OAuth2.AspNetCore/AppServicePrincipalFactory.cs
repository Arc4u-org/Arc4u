using System.Diagnostics;
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Arc4u.OAuth2;

/// <summary>
/// Creates an <see cref="AppPrincipal"/> on the server side. A token is requested from the <see cref="ITokenProvider"/> designated by the
/// <c>ProviderId</c> key of the settings, its claims are added to an identity, and the identity is transformed by the registered <see cref="IClaimsTransformation"/>.
/// </summary>
[Export(typeof(IAppPrincipalFactory))]
public class AppServicePrincipalFactory : IAppPrincipalFactory
{
    /// <summary>The settings key (<c>ProviderId</c>) that holds the key of the token provider.</summary>
    public const string ProviderKey = "ProviderId";

    private readonly IServiceProvider _container;
    private readonly ILogger<AppServicePrincipalFactory> _logger;
    private readonly IOptionsMonitor<SimpleKeyValueSettings> _settings;
    private readonly IClaimsTransformation _claimsTransformation;
    private readonly ActivitySource? _activitySource;

    /// <summary>Initializes a new instance of the <see cref="AppServicePrincipalFactory"/> class.</summary>
    /// <param name="container">The service provider used to resolve the token providers.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="settings">The named token provider settings.</param>
    /// <param name="claimsTransformation">The transformation that builds the <see cref="AppPrincipal"/>.</param>
    /// <param name="activitySourceFactory">The factory of the activity source used for telemetry.</param>
    public AppServicePrincipalFactory(IServiceProvider container, ILogger<AppServicePrincipalFactory> logger, IOptionsMonitor<SimpleKeyValueSettings> settings, IClaimsTransformation claimsTransformation, IActivitySourceFactory activitySourceFactory)
    {
        _container = container;
        _logger = logger;
        _settings = settings;
        _claimsTransformation = claimsTransformation;
        _activitySource = activitySourceFactory.GetArc4u();
    }

    /// <summary>
    /// Not supported: creating a principal requires the settings identifying the token provider.
    /// Use <see cref="CreatePrincipalAsync(string, object?)"/> or <see cref="CreatePrincipalAsync(IKeyValueSettings, object?)"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public Task<Result<AppPrincipal>> CreatePrincipalAsync(object? parameter = null)
    {
        throw new NotSupportedException($"Creating a principal requires the settings identifying the token provider. Use the overload taking a settings name or an {nameof(IKeyValueSettings)} instance.");
    }

    /// <inheritdoc/>
    public async Task<Result<AppPrincipal>> CreatePrincipalAsync(string settingsResolveName, object? parameter)
    {
        var settings = _settings.Get(settingsResolveName);
        return await CreatePrincipalAsync(settings, parameter).ConfigureAwait(false);
    }

    /// <summary>
    /// Create an <see cref="AppPrincipal"/> from a token obtained with the given settings.
    /// </summary>
    /// <param name="settings">The settings used to create the token.</param>
    /// <param name="parameter">Optional parameter forwarded to the token provider.</param>
    /// <returns>A <see cref="Result{AppPrincipal}"/>, failed when no token or no principal can be created.</returns>
    public async Task<Result<AppPrincipal>> CreatePrincipalAsync(IKeyValueSettings settings, object? parameter = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        using var activity = _activitySource?.StartActivity("Prepare the creation of the Arc4u Principal", ActivityKind.Producer);

        var identity = new ClaimsIdentity("OAuth2Bearer", "upn", ClaimsIdentity.DefaultRoleClaimType);

        var identityResult = await BuildTheIdentity(identity, settings, parameter).ConfigureAwait(false);
        if (identityResult.IsFailed)
        {
            return identityResult.ToResult<AppPrincipal>();
        }

        var principal = await _claimsTransformation.TransformAsync(new ClaimsPrincipal(identity)).ConfigureAwait(false);

        if (principal is not AppPrincipal appPrincipal)
        {
            return Result.Fail("No principal can be created: the claims transformation did not produce an AppPrincipal.");
        }

        return appPrincipal;
    }

    /// <summary>
    /// Fill the identity with the claims of the token obtained with the given settings.
    /// </summary>
    /// <param name="identity">The identity to fill.</param>
    /// <param name="settings">The settings used to create the token.</param>
    /// <param name="parameter">Optional parameter forwarded to the token provider.</param>
    /// <returns>A <see cref="Result"/>, failed when the provider is not registered or no token can be created.</returns>
    private async Task<Result> BuildTheIdentity(ClaimsIdentity identity, IKeyValueSettings settings, object? parameter = null)
    {
        if (!settings.Values.TryGetValue(ProviderKey, out var providerId))
        {
            return Result.Fail($"The settings do not contain the '{ProviderKey}' key identifying the token provider.");
        }

        var provider = _container.GetKeyedService<ITokenProvider>(providerId);
        if (provider is null)
        {
            return Result.Fail($"The token provider '{providerId}' is not registered.");
        }

        try
        {
            var tokenResult = await provider.GetTokenAsync(settings, parameter).ConfigureAwait(false);
            if (tokenResult.IsFailed)
            {
                tokenResult.Log();
                return tokenResult.ToResult();
            }

            identity.BootstrapContext = tokenResult.Value.Token;
            var jwtToken = new JsonWebToken(tokenResult.Value.Token);

            identity.AddMissingClaims(jwtToken.Claims.Select(c => (c.Type, c.Value)));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
            return Result.Fail(new ExceptionalError(ex));
        }
    }

    private async ValueTask RemoveCacheFromUserAsync(CancellationToken cancellationToken)
    {
        // IApplicationContext is scoped, so it is resolved per call instead of being injected in the factory.
        var appContext = _container.GetService<IApplicationContext>();
        if (appContext is null)
        {
            _logger.Technical().LogWarning("No application context is registered: the user claims cache cannot be cleared.");
            return;
        }

        if (appContext.Principal?.Identity is not ClaimsIdentity claimsIdentity)
        {
            _logger.Technical().LogError("No principal exists on the current context.");
            return;
        }

        var cacheHelper = _container.GetService<ICacheHelper>();
        var cacheKeyGenerator = _container.GetService<ICacheKeyGenerator>();

        if (cacheHelper is not null && cacheKeyGenerator is not null)
        {
            await cacheHelper.GetCache().RemoveAsync(cacheKeyGenerator.GetClaimsKey(claimsIdentity), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Removes the cached claims of the current user of the <see cref="IApplicationContext"/>.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the cache entry is removed.</returns>
    public ValueTask SignOutUserAsync(CancellationToken cancellationToken) => RemoveCacheFromUserAsync(cancellationToken);

    /// <summary>Removes the cached claims of the current user of the <see cref="IApplicationContext"/>. The settings are not used.</summary>
    /// <param name="settings">The token provider settings, which are ignored.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the cache entry is removed.</returns>
    public ValueTask SignOutUserAsync(IKeyValueSettings settings, CancellationToken cancellationToken) => RemoveCacheFromUserAsync(cancellationToken);
}
