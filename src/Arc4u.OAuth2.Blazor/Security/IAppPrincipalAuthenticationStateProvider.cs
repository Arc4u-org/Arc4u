#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Components.Authorization;

namespace Arc4u.Blazor;

/// <summary>Builds an authentication state whose user is an <see cref="Arc4u.Security.Principal.AppPrincipal"/> from serialized authentication data.</summary>
public interface IAppPrincipalAuthenticationStateProvider
{
    /// <summary>Builds the authentication state from the serialized data.</summary>
    /// <param name="authenticationStateData">The claims received from the server, or <see langword="null"/> for an anonymous user.</param>
    /// <returns>The authentication state.</returns>
    Task<AuthenticationState> DeserializeAuthenticationStateAsync(AuthenticationStateData? authenticationStateData);
}
#endif
