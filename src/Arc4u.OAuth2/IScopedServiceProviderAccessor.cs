namespace Arc4u.OAuth2;

/// <summary>
/// </summary>
public interface IScopedServiceProviderAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> of the current scope (for example the current HTTP request or Blazor circuit).
    /// </summary>
    IServiceProvider ServiceProvider { get; set; }
}
