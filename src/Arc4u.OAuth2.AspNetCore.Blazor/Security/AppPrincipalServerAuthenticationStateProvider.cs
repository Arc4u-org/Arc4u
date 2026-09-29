#if NET9_0_OR_GREATER
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Arc4u.Blazor;

/// <summary>
/// Server (Interactive Server / SignalR circuit) counterpart of the WebAssembly
/// <c>Arc4u.Blazor.AppPrincipalFromAuthenticationState</c> used on the client.
///
/// On the client, the <see cref="AppPrincipal"/> is rebuilt from the serialized
/// authentication state through the <c>DeserializationCallback</c> and pushed into the
/// circuit-scoped <see cref="IApplicationContext"/>.
///
/// On the server, <c>AppPrincipalTransform</c> (an <c>IClaimsTransformation</c>) runs
/// in the HTTP request scope during <c>UseAuthentication()</c>, so the <see cref="IApplicationContext"/>
/// it populates belongs to that request scope and is discarded before the interactive circuit runs.
/// The circuit uses its own DI scope, whose <see cref="IApplicationContext"/> is therefore empty.
///
/// This provider closes that gap: it derives from <see cref="ServerAuthenticationStateProvider"/>
/// (which the Blazor endpoint seeds from <c>HttpContext.User</c>) and, whenever the authentication
/// state is resolved inside the circuit, rebuilds the <see cref="AppPrincipal"/> and sets it on the
/// circuit-scoped <see cref="IApplicationContext"/> — mirroring the client behaviour.
/// </summary>
[Export(typeof(AuthenticationStateProvider)), Scoped]
public sealed class AppPrincipalServerAuthenticationStateProvider : ServerAuthenticationStateProvider
{
    private readonly IClaimProfileFiller _claimsFiller;
    private readonly IClaimAuthorizationFiller _authorizationFiller;
    private readonly IApplicationContext _applicationContext;

    public AppPrincipalServerAuthenticationStateProvider(
        IClaimProfileFiller claimsFiller,
        IClaimAuthorizationFiller authorizationFiller,
        IApplicationContext applicationContext)
    {
        _claimsFiller = claimsFiller;
        _authorizationFiller = authorizationFiller;
        _applicationContext = applicationContext;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // The base provider returns the state seeded from HttpContext.User when the circuit was established.
        var state = await base.GetAuthenticationStateAsync().ConfigureAwait(false);

        // Same construction as the WASM AppPrincipalFromAuthenticationState, reusing the identity
        // already carried by the state (it keeps its original authentication type and claims).
        var principal = ApplicationContextInitializer.SetPrincipal(
            state.User.Identity as ClaimsIdentity,
            _authorizationFiller,
            _claimsFiller,
            _applicationContext);

        // Anonymous user: leave the framework state untouched.
        return principal is null ? state : new AuthenticationState(principal);
    }

}
#endif
