using System.Security.Claims;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;

namespace Arc4u.Blazor;

/// <summary>
/// Builds an <see cref="AppPrincipal"/> from a <see cref="ClaimsIdentity"/> and pushes it into the
/// (scope-bound) <see cref="IApplicationContext"/>. This is the shared body used both by
/// <see cref="AppPrincipalServerAuthenticationStateProvider"/> (prerender / SSR request scope) and by
/// <see cref="ApplicationContextCircuitHandler"/> (interactive circuit scope), mirroring the WASM
/// <c>Arc4u.Blazor.AppPrincipalFromAuthenticationState</c>.
/// For an anonymous user, the principal of the <see cref="IApplicationContext"/> is cleared.
/// </summary>
internal static class ApplicationContextInitializer
{
    public static AppPrincipal? SetPrincipal(
        ClaimsIdentity? identity,
        IClaimAuthorizationFiller authorizationFiller,
        IClaimProfileFiller profileFiller,
        IApplicationContext applicationContext)
    {
        // Anonymous user: clear the context. A circuit that was signed in and is now anonymous (for example
        // reconnected without the cookie after a sign-out) must not keep acting as the previous user.
        if (identity is null || !identity.IsAuthenticated)
        {
            applicationContext.SetPrincipal(null);
            return null;
        }

        var authorization = authorizationFiller.GetAuthorization(identity);
        var profile = profileFiller.GetProfile(identity);
        var principal = new AppPrincipal(authorization, identity, profile.Sid) { Profile = profile };

        applicationContext.SetPrincipal(principal);

        return principal;
    }

    /// <summary>
    /// Sets the principal of the state received with the <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/> event:
    /// nothing reads the state again after a change, so the <see cref="IApplicationContext"/> must be updated from the event.
    /// Blazor raises the event with a completed task, so the principal is set before the event returns. A state that cannot be read is treated as an anonymous user.
    /// </summary>
    public static async Task SetPrincipalAsync(
        Task<AuthenticationState> authenticationState,
        IClaimAuthorizationFiller authorizationFiller,
        IClaimProfileFiller profileFiller,
        IApplicationContext applicationContext)
    {
        ClaimsIdentity? identity;
        try
        {
            var state = await authenticationState.ConfigureAwait(false);
            identity = state.User.Identity as ClaimsIdentity;
        }
        catch (Exception)
        {
            identity = null;
        }

        SetPrincipal(identity, authorizationFiller, profileFiller, applicationContext);
    }
}

