namespace Arc4u.Security.Principal;

/// <summary>
/// Gives access to the context of the running application: the current principal.
/// Depending on the implementation the context is a singleton (UI, single instance applications, unit tests) or scoped (ASP.NET Core back-ends).
/// </summary>
public interface IApplicationContext
{
    /// <summary>
    /// Gets the current principal, or <see langword="null"/> when nobody is authenticated.
    /// </summary>
    AppPrincipal? Principal { get; }

    /// <summary>
    /// Sets the current principal.
    /// </summary>
    /// <param name="principal">The principal, or <see langword="null"/> to clear it.</param>
    void SetPrincipal(AppPrincipal? principal);
}
