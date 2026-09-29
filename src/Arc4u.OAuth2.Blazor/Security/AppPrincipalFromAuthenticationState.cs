#if NET9_0_OR_GREATER
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;

namespace Arc4u.Blazor;

/// <summary>Rebuilds the <see cref="AppPrincipal"/> from the authentication state serialized by the Blazor server, and sets it in the <see cref="IApplicationContext"/>.</summary>
[Export(typeof(IAppPrincipalAuthenticationStateProvider)), Shared]
public class AppPrincipalFromAuthenticationState : IAppPrincipalAuthenticationStateProvider
{
    /// <summary>Initializes a new instance of the <see cref="AppPrincipalFromAuthenticationState"/> class.</summary>
    /// <param name="claimsFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="authorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="applicationContext">The application context that receives the principal.</param>
    public AppPrincipalFromAuthenticationState(IClaimProfileFiller claimsFiller, IClaimAuthorizationFiller authorizationFiller, IApplicationContext applicationContext)
    {
        _claimsFiller = claimsFiller;
        _authorizationFiller = authorizationFiller;
        _applicationContext = applicationContext;
    }

    private readonly IClaimProfileFiller _claimsFiller;
    private readonly IClaimAuthorizationFiller _authorizationFiller;
    private readonly IApplicationContext _applicationContext;
    /// <summary>Gets an authentication state for an anonymous user: a principal with an empty identity and no operation.</summary>
    public static AuthenticationState DefaultAuthenticationState => new AuthenticationState(new AppPrincipal(GetNoAuthorization(), new ClaimsIdentity(), "S-1-0-0" ));

    /// <inheritdoc/>
    public Task<AuthenticationState> DeserializeAuthenticationStateAsync(
        AuthenticationStateData? authenticationStateData)
    {
        if (authenticationStateData is null)
        {
            return Task.FromResult(DefaultAuthenticationState);
        }

        var identity = new ClaimsIdentity(authenticationStateData.Claims.Select(c => new Claim(c.Type, c.Value)), "Bearer");
        var authorization = _authorizationFiller.GetAuthorization(identity);
        var profile = _claimsFiller.GetProfile(identity);

        var principal = new AppPrincipal(authorization, identity, profile.Sid)
        {
            Profile = profile
        };
        _applicationContext.SetPrincipal(principal);
        return Task.FromResult(new AuthenticationState(principal));
    }


    private static Authorization GetNoAuthorization()
    {
        return new Authorization
        {
            Operations = [new ScopedOperations { Operations = [], Scope = string.Empty }],
            AllOperations = [],
            Roles = [new ScopedRoles { Roles = [], Scope = string.Empty }],
            Scopes = [string.Empty]
        };
    }
}
#endif
