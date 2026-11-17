using System.Security.Claims;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Blazor;

/// <summary>
/// Builds an <see cref="AppPrincipal"/> from a <see cref="ClaimsIdentity"/> and pushes it into the
/// (scope-bound) <see cref="IApplicationContext"/>. This is the shared body used both by
/// <see cref="AppPrincipalServerAuthenticationStateProvider"/> (prerender / SSR request scope) and by
/// <see cref="ApplicationContextCircuitHandler"/> (interactive circuit scope), mirroring the WASM
/// <c>Arc4u.Blazor.AppPrincipalFromAuthenticationState</c>.
/// For an anonymous user, the user is cleared (see <see cref="ClearUser"/>).
/// </summary>
internal static class ApplicationContextInitializer
{
    public static AppPrincipal? SetPrincipal(
        ClaimsIdentity? identity,
        IClaimAuthorizationFiller authorizationFiller,
        IClaimProfileFiller profileFiller,
        IApplicationContext applicationContext,
        IServiceProvider? scopeServices = null)
    {
        // Anonymous user: clear the context. A circuit that was signed in and is now anonymous (for example
        // reconnected without the cookie after a sign-out) must not keep acting as the previous user.
        if (identity is null || !identity.IsAuthenticated)
        {
            ClearUser(applicationContext, scopeServices);
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
        IApplicationContext applicationContext,
        IServiceProvider? scopeServices = null)
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

        SetPrincipal(identity, authorizationFiller, profileFiller, applicationContext, scopeServices);
    }

    /// <summary>
    /// Removes the user from the scope: the principal of <paramref name="applicationContext"/> and, when <paramref name="scopeServices"/> is given,
    /// the user's tokens (<see cref="TokenRefreshInfo"/>) of that scope and the principal and tokens of the scope that
    /// <see cref="IScopedServiceProviderAccessor"/> resolves.
    /// </summary>
    /// <remarks>
    /// In a circuit, the two scopes are not the same. The accessor falls back to the request scope of the SignalR connection, whose
    /// <see cref="IApplicationContext"/> the claims transformation filled and whose <see cref="TokenRefreshInfo"/> the cookie events filled when
    /// the connection opened. The <c>JwtHttpHandler</c> and the gRPC interceptor read them there: clearing only the circuit's principal would let
    /// them keep calling APIs with the user's access token, and keep refreshing it with the refresh token, after the sign-in has ended.
    /// This runs in the flow of the connection that last set the circuit's state, so the accessor finds that connection's scope; once the
    /// connection has closed there is nothing left to clear there.
    /// </remarks>
    public static void ClearUser(IApplicationContext applicationContext, IServiceProvider? scopeServices)
    {
        applicationContext.SetPrincipal(null);

        if (scopeServices is null)
        {
            return;
        }

        ClearTokens(scopeServices);

        if (ConnectionServices(scopeServices) is not { } connectionServices || ReferenceEquals(connectionServices, scopeServices))
        {
            return;
        }

        try
        {
            if (connectionServices.GetService<IApplicationContext>() is { } connectionContext && !ReferenceEquals(connectionContext, applicationContext))
            {
                connectionContext.SetPrincipal(null);
            }
        }
        catch (ObjectDisposedException)
        {
            // The connection's request scope has already been disposed.
            return;
        }

        ClearTokens(connectionServices);
    }

    private static void ClearTokens(IServiceProvider services)
    {
        try
        {
            if (services.GetService<TokenRefreshInfo>() is { } tokens)
            {
                tokens.AccessToken = null;
                tokens.RefreshToken = null;
                tokens.IdentityToken = null;
            }
        }
        catch (ObjectDisposedException)
        {
            // The scope has already been disposed: its tokens are gone with it.
        }
    }

    private static IServiceProvider? ConnectionServices(IServiceProvider scopeServices)
    {
        try
        {
            return scopeServices.GetService<IScopedServiceProviderAccessor>()?.ServiceProvider;
        }
        catch (NullReferenceException)
        {
            // Arc4u's accessor throws this when the flow has neither a scope nor an HTTP context.
            return null;
        }
        catch (ObjectDisposedException)
        {
            // The connection's request scope has already been disposed.
            return null;
        }
    }
}
