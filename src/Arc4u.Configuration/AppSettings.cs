using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Microsoft.Extensions.Configuration;

namespace Arc4u;

/// <summary>
/// The application-wide <see cref="IAppSettings"/>: the key/value pairs found in the <c>AppSettings</c> section of the configuration.
/// It is registered as a singleton through the <c>Export</c>/<c>Shared</c> attributes.
/// </summary>
[Export(typeof(IAppSettings)), Shared]
public sealed class AppSettings : KeyValueSettings, IAppSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettings"/> class from the <c>AppSettings</c> section of <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">The configuration containing the <c>AppSettings</c> section.</param>
    public AppSettings(IConfiguration configuration)
        : base("AppSettings", configuration)
    {
    }
}
