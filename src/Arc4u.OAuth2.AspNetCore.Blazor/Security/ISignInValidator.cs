using System.Security.Claims;

namespace Arc4u.Blazor;

/// <summary>
/// Tells whether the sign-in behind a principal still exists. <see cref="AppPrincipalServerAuthenticationStateProvider"/> asks it
/// periodically for the user of each open circuit. The default implementation is <see cref="SessionStoreSignInValidator"/>; register
/// another one to check the sign-in elsewhere (for example at the identity provider).
/// </summary>
public interface ISignInValidator
{
    /// <summary>Tells whether the sign-in behind <paramref name="user"/> still exists.</summary>
    /// <param name="user">The user of the circuit.</param>
    /// <param name="cancellationToken">A token to cancel the check (the circuit is closing).</param>
    /// <returns><see langword="true"/> when the sign-in still exists; <see langword="false"/> to turn the circuit anonymous. An exception also turns it anonymous.</returns>
    Task<bool> IsSignInAliveAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}
