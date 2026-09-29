using System.Diagnostics;
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2;

/// <summary>
/// Transform a <see cref="ClaimsPrincipal"/> to a <see cref="AppPrincipal"/>.
/// This needs to be injected with a scoped lifetime, because it relies on an <see cref="IApplicationContext"/>, which is also scoped.
/// </summary>
[Export(typeof(IClaimsTransformation)), Scoped]
public class AppPrincipalTransform : IClaimsTransformation
{
    // Placeholder SID for the temporary principal set on the context while the real one is being built.
    private const string DummySid = "S-1-0-0";

    private readonly IClaimProfileFiller _claimProfileFiller;
    private readonly IClaimAuthorizationFiller _claimAuthorizationFiller;
    private readonly ICacheHelper _cacheHelper;
    private readonly ActivitySource? _activitySource;
    private readonly IApplicationContext _applicationContext;
    private readonly ClaimsFillerOptions _options;
    private readonly ICacheKeyGenerator? _cacheKeyGenerator;
    private readonly IClaimsFiller? _claimsFiller;
    private readonly ILogger<AppPrincipalTransform> _logger;
    private readonly TokenCacheOptions _cacheOptions;

    /// <summary>Initializes a new instance of the <see cref="AppPrincipalTransform"/> class.</summary>
    /// <param name="applicationContext">The scoped application context that receives the principal.</param>
    /// <param name="claimProfileFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="claimAuthorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="serviceProvider">The service provider, used to resolve the claims filler and the cache key generator when extra claims are loaded.</param>
    /// <param name="options">The claims filler options.</param>
    /// <param name="cacheHelper">The helper giving access to the token cache.</param>
    /// <param name="activitySourceFactory">The factory of the activity source used for telemetry.</param>
    /// <param name="tokenCacheOptions">The token cache options, giving the lifetime of the cached claims.</param>
    /// <param name="logger">The logger.</param>
    public AppPrincipalTransform(
        IApplicationContext applicationContext,
        IClaimProfileFiller claimProfileFiller,
        IClaimAuthorizationFiller claimAuthorizationFiller,
        IServiceProvider serviceProvider,
        IOptions<ClaimsFillerOptions> options,
        ICacheHelper cacheHelper,
        IActivitySourceFactory activitySourceFactory,
        IOptions<TokenCacheOptions> tokenCacheOptions,
        ILogger<AppPrincipalTransform> logger)
    {
        _claimProfileFiller = claimProfileFiller;
        _claimAuthorizationFiller = claimAuthorizationFiller;
        _applicationContext = applicationContext;
        _options = options.Value;
        _activitySource = activitySourceFactory.GetArc4u();
        _logger = logger;
        _cacheHelper = cacheHelper;
        _cacheOptions = tokenCacheOptions.Value;

        if (_options.LoadClaimsFromClaimsFillerProvider)
        {
            _claimsFiller = serviceProvider.GetRequiredService<IClaimsFiller>();

            _cacheKeyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
        }
    }

    /// <summary>
    /// Builds an <see cref="AppPrincipal"/> from the principal and stores it in the <see cref="IApplicationContext"/>.
    /// When <see cref="ClaimsFillerOptions.LoadClaimsFromClaimsFillerProvider"/> is enabled, the extra claims are first loaded from the cache or from the registered
    /// <see cref="IClaimsFiller"/> and added to the identity, except those listed in <see cref="ClaimsFillerOptions.ClaimsToExclude"/>.
    /// </summary>
    /// <param name="principal">The principal to transform.</param>
    /// <returns>The <see cref="AppPrincipal"/> built from the identity of <paramref name="principal"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The principal has no identity.</exception>
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity is null)
        {
            throw new ArgumentException("The principal must carry an identity.", nameof(principal));
        }

        _logger.Technical().LogPrincipalCreation();

        // Add Telemetry.
        using var activity = _activitySource?.StartActivity("Create Arc4u Principal", ActivityKind.Producer);

        // As the extension point can use some ITokenProvider based on the user.
        // A dummy Principal is created based on the context identity!
        // Must be registered as Scoped!
        _applicationContext.SetPrincipal(new AppPrincipal(new Authorization(), principal.Identity, DummySid));

        // Load Claims from an external source if necessary!
        if (_options.LoadClaimsFromClaimsFillerProvider)
        {
            if (principal.Identity is ClaimsIdentity claimsIdentity)
            {
                await LoadExtraClaimsAsync(claimsIdentity).ConfigureAwait(false);
            }
            else
            {
                _logger.Technical().LogWarning("Extra claims can only be loaded on a ClaimsIdentity: the claims filler is skipped.");
            }
        }

        // Build an AppPrincipal.
        var authorization = _claimAuthorizationFiller.GetAuthorization(principal.Identity);
        var profile = _claimProfileFiller.GetProfile(principal.Identity);
        var appPrincipal = new AppPrincipal(authorization, principal.Identity, profile.Sid) { Profile = profile };

        _applicationContext.SetPrincipal(appPrincipal);

        return appPrincipal;
    }

    #region Handling extra claims

    /// <summary>
    /// Load extra claims from the IClaimsFiller and add them to the identity.
    /// This method is called only if the LoadClaimsFromClaimsFillerProvider option is set to true.
    /// </summary>
    /// <param name="identity">The identity to enrich.</param>
    private async Task LoadExtraClaimsAsync(ClaimsIdentity identity)
    {
        // _cacheKeyGenerator and _claimsFiller are non-null when LoadClaimsFromClaimsFillerProvider is true (enforced in the constructor).
        var cacheKey = _cacheKeyGenerator!.GetClaimsKey(identity);

        // Something already in the cache? Avoid an expensive backend call.
        var cachedClaims = GetClaimsFromCache(cacheKey);

        if (cachedClaims.Count > 0)
        {
            identity.AddMissingClaims(cachedClaims.Select(c => (c.ClaimType, c.Value)));

            return;
        }

        // Add Telemetry.
        using var activity = _activitySource?.StartActivity("Fetch extra claims.", ActivityKind.Producer);

        var claimsToExclude = _options.ClaimsToExclude.ToHashSet(StringComparer.Ordinal);

        // Should receive specific extra claims. This is the responsibility of the caller to provide the right claims.
        var claims = (await _claimsFiller!.GetAsync(identity).ConfigureAwait(false))
            .Where(c => !claimsToExclude.Contains(c.ClaimType)).ToList();

        identity.AddMissingClaims(claims.Select(c => (c.ClaimType, c.Value)));

        SaveClaimsToCache(claims, cacheKey);
    }

    private List<ClaimDto> GetClaimsFromCache(string cacheKey)
    {
        try
        {
            return _cacheHelper.GetCache().Get<List<ClaimDto>>(cacheKey) ?? [];
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
            return [];
        }
    }

    private void SaveClaimsToCache(IEnumerable<ClaimDto> claims, string cacheKey)
    {
        try
        {
            _cacheHelper.GetCache().Put(cacheKey, _cacheOptions.MaxTime, claims);
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
        }
    }

    #endregion
}
