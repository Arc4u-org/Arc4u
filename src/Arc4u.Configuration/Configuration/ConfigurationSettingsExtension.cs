using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration;

// Extension class to register a key/value with IOption<T>...
/// <summary>
/// Extension methods to register a section of the configuration as named key/value settings.
/// </summary>
public static class ConfigurationSettingsExtension
{
    /// <summary>
    /// Register a key/value collection with IOption model, only taking the properties that map to strings.
    /// </summary>
    /// <param name="services">The service collection <see cref="IServiceCollection"/></param>
    /// <param name="name">The name to get the back the DIctionary.</param>
    /// <param name="configuration"><see cref="IConfiguration"/> to read the settings.</param>
    /// <param name="sectionName">The section name containing the keys and values.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ConfigurationException"></exception>
    public static IKeyValueSettings ConfigureSettings(this IServiceCollection services, string name, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sectionName);

        // Get the specified `sectionName` from the `configuration` parameter.
        var section = configuration.GetSection(sectionName);

        // Check if the `section` exists in the `configuration`.
        if (!section.Exists())
        {
            // Throw a `ConfigurationException` if the `section` doesn't exist.
            throw new ConfigurationException($"No section with name {sectionName} exists.");
        }

        // Get a `Dictionary<string, string>` from the `section`.
        var dic = section.GetChildren()?.ToDictionary(x => x.Key, x => x.Value!) ?? throw new ArgumentException($"Section {sectionName} doesn't contain a usable string dictionary", nameof(sectionName));

        // Define a local method `options` that takes a `Dictionary<string, string>` parameter `o` and adds each key-value pair
        // from the retrieved dictionary `dic` to it.
        void options(SimpleKeyValueSettings o)
        {
            foreach (var kv in dic)
            {
                o.Add(kv.Key, kv.Value);
            }
        }

        // Configure the specified `services` parameter with the `OAuth2` section and the `options` method.
        services.Configure<SimpleKeyValueSettings>(name, options);

        var result = new SimpleKeyValueSettings();
        options(result);

        return result;
    }
}

