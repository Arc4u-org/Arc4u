using Arc4u.Dependency.Attribute;

namespace Arc4u.Security.Principal;

/// <summary>
/// This class can be registered manually in your DI container.
/// 1) as Singleton when used:
///     a) in UI.
///     b) in one instance application context
///     c) unit test.
/// 2) as Scoped in AspNetCore backend or backend unit test..
/// The Export must only be used in case 1.
/// </summary>
[Export(typeof(IApplicationContext)), Shared]
public class ApplicationInstanceContext : IApplicationContext
{
    /// <inheritdoc/>
    public void SetPrincipal(AppPrincipal? principal)
    {
        Principal = principal;
    }

    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    /// <value>The activity ID.</value>
    public string ActivityID { get; set; } = string.Empty;

    /// <inheritdoc/>
    public AppPrincipal? Principal { get; private set; }
}
