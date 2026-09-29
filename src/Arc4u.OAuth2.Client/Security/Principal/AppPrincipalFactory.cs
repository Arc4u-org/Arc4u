using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Caching;
using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.IdentityModel.Claims;
using Arc4u.Network.Connectivity;
using Arc4u.OAuth2.Token;
using Arc4u.Results.Validation;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Client.Security.Principal;

/// <summary>
/// Creates an <see cref="AppPrincipal"/> on the client side. A token is requested from the <see cref="ITokenProvider"/> designated by the <c>ProviderId</c> key of the settings, its claims
/// (and the ones given by the registered <see cref="IClaimsFiller"/>) are added to an identity and cached; the authorization and profile are then built from the claims.
/// Without network, the principal is built from the cached claims. The principal is set in the <see cref="IApplicationContext"/>.
/// </summary>
/// <param name="container">The service provider used to resolve the token providers and the claim fillers.</param>
/// <param name="networkInformation">The network status.</param>
/// <param name="claimsCache">The secure cache in which the claims are kept.</param>
/// <param name="cacheKeyGenerator">The generator of the cache key of the claims.</param>
/// <param name="applicationContext">The application context that receives the principal.</param>
/// <param name="logger">The logger.</param>
[Export(typeof(IAppPrincipalFactory))]
public class AppPrincipalFactory(IServiceProvider container, INetworkInformation networkInformation, ISecureCache claimsCache, ICacheKeyGenerator cacheKeyGenerator, IApplicationContext applicationContext, ILogger<AppPrincipalFactory> logger) : IAppPrincipalFactory
{
    private const string ProviderKey = "ProviderId";
    private const string DefaultSettingsResolveName = "OAuth2";
    /// <summary>The name (<c>platformParameters</c>) of the platform specific parameters.</summary>
    public const string PlatformParameters = "platformParameters";

    private static readonly string tokenExpirationClaimType = "exp";
    private static readonly string[] ClaimsToExclude = [ "aud", "iss", "iat", "nbf", "acr", "aio", "appidacr", "ipaddr", "scp", "tid", "uti", "unique_name", "apptype", "appid", "ver" ];
    private readonly ICache _claimsCache = claimsCache;

    /// <summary>Creates the principal with the settings named <c>OAuth2</c>.</summary>
    /// <param name="parameter">Optional parameter forwarded to the token provider.</param>
    /// <returns>The principal, or a failed result.</returns>
    public async Task<Result<AppPrincipal>> CreatePrincipalAsync(object? parameter = null)
    {
        return await CreatePrincipalAsync(DefaultSettingsResolveName, parameter).ConfigureAwait(true);
    }

    /// <summary>Creates the principal with the named settings.</summary>
    /// <param name="settingsResolveName">The name of the key/value settings.</param>
    /// <param name="parameter">Optional parameter forwarded to the token provider.</param>
    /// <returns>The principal, or a failed result when the settings do not exist or the principal cannot be built.</returns>
    public async Task<Result<AppPrincipal>> CreatePrincipalAsync(string settingsResolveName, object? parameter = null)
    {
        var settings = container.GetKeyedService<IKeyValueSettings>(settingsResolveName);

        if (settings == null)
        {
            return Result.Fail($"No section {settingsResolveName} was found.");
        }

        return await CreatePrincipalAsync(settings, parameter).ConfigureAwait(false);
    }

    /// <summary>Creates the principal with the given settings.</summary>
    /// <param name="settings">The settings, which must contain the <c>ProviderId</c> key.</param>
    /// <param name="parameter">Optional parameter forwarded to the token provider.</param>
    /// <returns>The principal, or a failed result when the principal cannot be built.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public async Task<Result<AppPrincipal>> CreatePrincipalAsync(IKeyValueSettings settings, object? parameter = null)
    {
        var result = new Result<AppPrincipal>();
        var identity = new ClaimsIdentity("OAuth2Bearer", System.Security.Claims.ClaimTypes.Upn, ClaimsIdentity.DefaultRoleClaimType);

        ArgumentNullException.ThrowIfNull(settings);

        // when we have no internet connectivity may be we have claims in cache.
        if (NetworkStatus.None == networkInformation.Status)
        {
            // In a scenario where the claims cached are always for one user like a UI, the identity is not used => so retrieving the claims in the cache is possible!
            var emptyIdentity = new ClaimsIdentity();
            var cachedClaims = GetClaimsFromCache(emptyIdentity);
            identity.AddClaims(cachedClaims.Select(p => new Claim(p.ClaimType, p.Value)));
            result.WithSuccess("Create the principal from the cache due to no network connectivity.");
        }
        else
        {
            var IdentityResult = await BuildTheIdentityAsync(identity, settings, parameter).ConfigureAwait(false);
            result.WithReasons(IdentityResult.Reasons);
        }

        if (result.IsFailed)
        {
            return result;
        }

        var authorizationResult = BuildAuthorization(identity);
        var profileResult = BuildProfile(identity);

        result.WithReasons(authorizationResult.Reasons).WithReasons(profileResult.Reasons);

        if (result.IsFailed)
        {
            return result;
        }

        var principal = new AppPrincipal(authorizationResult.Value, identity, "S-1-0-0")
        {
            Profile = profileResult.Value
        };

        applicationContext.SetPrincipal(principal);

        return result.WithValue(principal);
    }

    private async Task<Result> BuildTheIdentityAsync(ClaimsIdentity identity, IKeyValueSettings settings, object? parameter = null)
    {
        // Check if we have a provider registered.
        if (!container.TryGetService(settings.Values[ProviderKey], out ITokenProvider? provider))
        {
            return Result.Fail($"The principal cannot be created. We are missing an account provider: {settings.Values[ProviderKey]}");
        }

        var result = new Result();
        // Check the settings contains the service url.
        Result<TokenInfo> tokenResult = new();
        try
        {
            tokenResult = await provider!.GetTokenAsync(settings, parameter).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            tokenResult = new ExceptionalError(ex);
        }

        if (tokenResult.IsSuccess)
        {
            // The token has claims filled by the STS.
            // We can fill the new federated identity with the claims from the token.
            var jwtToken = new JwtSecurityToken(tokenResult.Value.Token);
            var expTokenClaim = jwtToken.Claims.FirstOrDefault(c => c.Type.Equals(tokenExpirationClaimType, StringComparison.InvariantCultureIgnoreCase));
            long expTokenTicks = 0;
            if (null != expTokenClaim)
            {
                long.TryParse(expTokenClaim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out expTokenTicks);
            }

            // The key for the cache is based on the claims from a ClaimsIdentity => build a dummy identity with the claim from the token.
            var dummyIdentity = new ClaimsIdentity(jwtToken.Claims);
            var cachedClaims = GetClaimsFromCache(dummyIdentity);

            identity.BootstrapContext = tokenResult.Value.Token;

            // if we have a token "cached" from the system, we can take the authorization claims from the cache (if exists)...
            // so we avoid too many backend calls for nothing.
            // But every time we have a token that has been refreshed, we will call the backend (if available and reload the claims).
            var cachedExpiredClaim = cachedClaims.FirstOrDefault(c => c.ClaimType.Equals(tokenExpirationClaimType, StringComparison.InvariantCultureIgnoreCase));
            long cachedExpiredTicks = 0;

            if (null != cachedExpiredClaim)
            {
                long.TryParse(cachedExpiredClaim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out cachedExpiredTicks);
            }

            // we only call the backend if the ticks are not the same.
            var copyClaimsFromCache = cachedExpiredTicks > 0 && expTokenTicks > 0 && cachedClaims.Count > 0 && cachedExpiredTicks == expTokenTicks;

            if (copyClaimsFromCache)
            {
                identity.AddClaims(cachedClaims.Select(p => new Claim(p.ClaimType, p.Value)));
                result.WithSuccess("Create the principal from the cache, token has not been refreshed.");
            }
            else
            {
                // Fill the claims based on the token and the backend call
                identity.AddClaims(jwtToken.Claims.Where(c => !ClaimsToExclude.Any(arg => arg.Equals(c.Type))).Select(c => new Claim(c.Type, c.Value)));
                if (null != expTokenClaim)
                {
                    identity.AddClaim(expTokenClaim);
                }

                if (container.TryGetService(out IClaimsFiller? claimFiller)) // Fill the claims with more information.
                {
                    try
                    {
                        // Get the claims and clean any technical claims in case of.
                        var claims = (await claimFiller!.GetAsync(identity).ConfigureAwait(false))
                                        .Where(c => !ClaimsToExclude.Any(arg => arg.Equals(c.ClaimType))).ToList();

                        // We copy the claims from the backend but the exp claim will be the value of the token (front end definition) and not the backend one. Otherwhise there will be always a difference.
                        identity.AddClaims(claims.Where(c => !identity.Claims.Any(c1 => c1.Type == c.ClaimType)).Select(c => new Claim(c.ClaimType, c.Value)));

                        result.WithSuccess($"Add {claims.Count} claims to the principal.");
                        result.WithSuccess($"Save claims to the cache.");
                    }
                    catch (Exception e)
                    {
                        result.WithError(new ExceptionalError(e));
                    }
                }

                SaveClaimsToCache(identity);
            }
        }
        else
        {
            result.WithError("The call to identify the user has failed. Token is null!");
        }

        return result;
    }

    /// <summary>
    /// Based on the token provider Id, the method will call the token provider and build a claimPrincipal!
    /// The provider id is the string used by the Composition library to register the type and not the provider Id used by the token provider itself (Microsoft, google, or other...).
    /// Today only the connected scenario is covered!
    /// </summary>
    private Result<Authorization> BuildAuthorization(ClaimsIdentity identity)
    {
        // We need to fill the authorization and user profile from the provider!
        if (container.TryGetService(out IClaimAuthorizationFiller? claimAuthorizationFiller))
        {
            var authorization = claimAuthorizationFiller!.GetAuthorization(identity);
            return Result.Ok(authorization).WithSuccess("Fill the authorization information to the principal.");
        }
        else
        {
            return ValidationError.Create("No class was found to fill the authorization to the principal.").WithSeverity(Severity.Warning);
        }
    }

    private Result<UserProfile> BuildProfile(ClaimsIdentity identity)
    {
        if (container.TryGetService(out IClaimProfileFiller? profileFiller))
        {
            var profile = profileFiller!.GetProfile(identity);
            return Result.Ok(profile).WithSuccess("Fill the profile information to the principal.");
        }
        else
        {
            return ValidationError.Create("No class was found to fill the principal profile.").WithSeverity(Severity.Warning);
        }
    }

    private List<ClaimDto> GetClaimsFromCache(ClaimsIdentity identity)
    {
        try
        {
            var secureClaims = _claimsCache.Get<List<ClaimDto>>(cacheKeyGenerator.GetClaimsKey(identity));
            return secureClaims ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private void SaveClaimsToCache(ClaimsIdentity identity)
    {
        var claimsDto = identity.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList();

        try
        {
            _claimsCache.Put(cacheKeyGenerator.GetClaimsKey(identity), claimsDto);
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
        }
    }

    private void RemoveClaimsCache()
    {
        try
        {
            // In a scenario where the claims cached are always for one user like a UI, the identity is not used
            var emptyIdentity = new ClaimsIdentity();
            _claimsCache.Remove(cacheKeyGenerator.GetClaimsKey(emptyIdentity));
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
        }
    }

    /// <summary>Signs the user out with the settings named <c>OAuth2</c>.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is signed out.</returns>
    /// <exception cref="InvalidOperationException">No settings named <c>OAuth2</c> exist.</exception>
    public ValueTask SignOutUserAsync(CancellationToken cancellationToken)
    {
        var settings = container.GetKeyedService<IKeyValueSettings>(DefaultSettingsResolveName);

        if (null == settings)
        {
            throw new InvalidOperationException($"No section {DefaultSettingsResolveName} was found.");
        }

        return SignOutUserAsync(settings, cancellationToken);
    }

    /// <summary>Removes the cached claims and signs the user out of the token provider.</summary>
    /// <param name="settings">The settings, which must contain the <c>ProviderId</c> key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is signed out.</returns>
    public async ValueTask SignOutUserAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        RemoveClaimsCache();
        if (container.TryGetService(settings.Values[ProviderKey], out ITokenProvider? provider))
        {
            await provider!.SignOutAsync(settings, cancellationToken).ConfigureAwait(false);
        }
    }
}
