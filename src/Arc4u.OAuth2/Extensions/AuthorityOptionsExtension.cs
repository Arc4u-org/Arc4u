using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the <see cref="AuthorityOptions"/> of the identity providers. The authorities are named options; the one used by default is named <c>Default</c>.</summary>
public static class AuthorityOptionsExtension
{
    /// <summary>Registers the authority named <c>Default</c> from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the authority.</param>
    /// <example>
    /// <code language="csharp">
    /// services.AddDefaultAuthority(o => o.SetData(new Uri("https://login.example.com/tenant"), null, null, null));
    /// </code>
    /// </example>
    public static void AddDefaultAuthority(this IServiceCollection services, Action<AuthorityOptions> options)
    {
        services.AddAuthority(options, "Default");
    }

    /// <summary>Registers the authority named <c>Default</c> from a configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section describing the authority (<c>Url</c>, <c>TokenEndpoint</c>, <c>Issuer</c>, <c>MetaDataAddress</c>). The default is <c>Authentication:DefaultAuthority</c>.</param>
    /// <exception cref="ConfigurationException">The section does not exist or cannot be bound.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddDefaultAuthority(configuration);
    /// </code>
    /// </example>
    public static void AddDefaultAuthority(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication:DefaultAuthority")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(sectionName);
        }

        var section = configuration.GetSection(sectionName);

        if (!section.Exists())
        {
            throw new ConfigurationException($"Section {sectionName} doesn't exist");
        }

        var option = section.Get<AuthorityOptions>();

        if (option is null)
        {
            throw new ConfigurationException($"Section {sectionName} doesn't correspond to the expected format.");
        }

        services.AddDefaultAuthority(options =>
        {
            options.SetData(option.Url, option.TokenEndpoint, option.Issuer, option.MetaDataAddress);
        });
    }

    /// <summary>Registers a named authority from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the authority.</param>
    /// <param name="optionKey">The name of the authority. Token providers select it with the <c>Authority</c> key of their settings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="options"/> is <see langword="null"/>, or <paramref name="optionKey"/> is empty.</exception>
    public static void AddAuthority(this IServiceCollection services, Action<AuthorityOptions> options, string optionKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(optionKey))
        {
            throw new ArgumentNullException(optionKey);
        }

        // validation.
        var extract = new AuthorityOptions();
        options(extract);

        if (extract.Url is null)
        {
            throw new ConfigurationException("Url authority field is mandatory.");
        }

        services.Configure(optionKey, options);
    }

}
