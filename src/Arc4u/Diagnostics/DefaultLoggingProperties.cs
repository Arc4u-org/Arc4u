using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;

namespace Arc4u.Diagnostics;

/// <summary>
/// The default <see cref="IAddPropertiesToLog"/>: it adds the activity identifier and the identity name of the current
/// <see cref="IApplicationContext"/> to the log entries. It is registered as a scoped service under the key <c>Scoped</c>.
/// </summary>
[Export("Scoped", typeof(IAddPropertiesToLog)), Scoped]
public class DefaultLoggingProperties : IAddPropertiesToLog
{
    private readonly IApplicationContext applicationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLoggingProperties"/> class.
    /// </summary>
    /// <param name="applicationContext">The application context that provides the activity identifier and the current principal.</param>
    public DefaultLoggingProperties(IApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
    }

    /// <summary>
    /// Gets the properties to add to the log entries.
    /// </summary>
    /// <returns>
    /// A dictionary containing the activity identifier and the identity (the profile name if there is one, otherwise the identity name of the principal)
    /// when the application context has a principal; otherwise an empty dictionary.
    /// </returns>
    public IDictionary<string, object> GetProperties()
    {
        if (null != applicationContext)
        {
            if (null != applicationContext.Principal)
            {
                return new Dictionary<string, object>
                    {
                        { LoggingConstants.ActivityId, applicationContext.ActivityID },
                        { LoggingConstants.Identity, (null != applicationContext.Principal?.Profile)
                                                                                        ? applicationContext.Principal.Profile.Name ?? string.Empty
                                                                                        : null != applicationContext.Principal?.Identity ? applicationContext.Principal.Identity.Name ?? string.Empty: string.Empty }
                    };
            }
        }

        return new Dictionary<string, object>();

    }
}
