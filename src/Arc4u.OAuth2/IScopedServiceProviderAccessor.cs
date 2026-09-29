namespace Arc4u.OAuth2;

/// <summary>
/// Gives access to the <see cref="IServiceProvider"/> of the current scope (for example the current HTTP request or Blazor circuit),
/// so scoped services can be resolved by code that lives longer than the scope.
/// </summary>
public interface IScopedServiceProviderAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> of the current scope (for example the current HTTP request or Blazor circuit).
    /// </summary>
    IServiceProvider ServiceProvider { get; set; }
}
