using Arc4u.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the mapping between domains, stored as named <see cref="SimpleKeyValueSettings"/>.</summary>
public static class DomainMappingExtensions
{
    /// <summary>Registers a domain mapping from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that fills the mapping.</param>
    /// <param name="sectionKey">The name of the settings. The default is <c>DomainMapping</c>.</param>
    public static void AddDomainMapping(this IServiceCollection services, Action<SimpleKeyValueSettings> options, string sectionKey = "DomainMapping")
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sectionKey);

        services.Configure<SimpleKeyValueSettings>(sectionKey, options);
    }

    /// <summary>Registers a domain mapping from a configuration section of key/value pairs.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the pairs. The default is <c>Authentication:DomainsMapping</c>. An empty mapping is registered when the section does not exist.</param>
    /// <param name="sectionKey">The name of the settings. The default is <c>DomainMapping</c>.</param>
    public static void AddDomainMapping(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication:DomainsMapping", string sectionKey = "DomainMapping")
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName));
        }
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            throw new ArgumentNullException(nameof(sectionKey));
        }
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);

        var settings = (section is null || !section.Exists()) ? new Dictionary<string, string>() : section.Get<Dictionary<string, string>>();

        settings ??= new Dictionary<string, string>();

        AddDomainMapping(services, options =>
        {
            foreach (var key in settings.Keys)
            {
                options.Add(key, settings[key]);
            }
        }, sectionKey);
    }
}
