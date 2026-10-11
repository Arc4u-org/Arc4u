using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;

namespace Arc4u.Diagnostics;

/// <summary>
/// The default <see cref="IAddPropertiesToLog"/>: it adds the user of the current <see cref="IApplicationContext"/> to the log entries,
/// as the email of its profile (a claim every identity provider can give), or its identity name when there is no email. It is registered as a scoped service under the key <c>Scoped</c>.
/// </summary>
[Export("Scoped", typeof(IAddPropertiesToLog)), Scoped]
public class DefaultLoggingProperties : IAddPropertiesToLog
{
    private readonly IApplicationContext applicationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLoggingProperties"/> class.
    /// </summary>
    /// <param name="applicationContext">The application context that provides the current principal.</param>
    public DefaultLoggingProperties(IApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
    }

    /// <summary>
    /// Gets the properties to add to the log entries.
    /// </summary>
    /// <returns>
    /// A dictionary containing the identity (the email of the profile, or the identity name of the principal when the identity provider gave no email)
    /// when the application context has a principal; otherwise an empty dictionary.
    /// </returns>
    public IDictionary<string, object> GetProperties()
    {
        var principal = applicationContext?.Principal;
        if (principal is null)
        {
            return new Dictionary<string, object>();
        }

        var identity = principal.Profile?.Email;
        if (string.IsNullOrWhiteSpace(identity))
        {
            identity = principal.Identity?.Name ?? string.Empty;
        }

        return new Dictionary<string, object> { { LoggingConstants.Identity, identity } };
    }
}
