using System.Security.Claims;
using Arc4u.Dependency.Attribute;

namespace Arc4u.Security.Principal;

/// <summary>
/// An <see cref="IApplicationContext"/> that stores the principal in <see cref="Thread.CurrentPrincipal"/>
/// and reads it from <see cref="System.Security.Claims.ClaimsPrincipal.Current"/>.
/// </summary>
[Export(typeof(IApplicationContext)), Shared]
public class ApplicationClaimsPrincipalSelectorContext : IApplicationContext
{
    /// <summary>
    /// Gets the current principal when <see cref="System.Security.Claims.ClaimsPrincipal.Current"/> is an <see cref="AppPrincipal"/>; otherwise <see langword="null"/>.
    /// </summary>
    public AppPrincipal? Principal => ClaimsPrincipal.Current as AppPrincipal;

    /// <summary>
    /// Sets <see cref="Thread.CurrentPrincipal"/>.
    /// </summary>
    /// <param name="principal">The principal, or <see langword="null"/>.</param>
    public void SetPrincipal(AppPrincipal? principal)
    {
        Thread.CurrentPrincipal = principal;
    }
}
