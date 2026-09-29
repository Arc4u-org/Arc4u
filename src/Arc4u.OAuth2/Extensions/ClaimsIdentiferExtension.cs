using System.Diagnostics.CodeAnalysis;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the <see cref="ClaimsIdentifierOption"/>, the claim types used to identify a user.</summary>
public static class ClaimsidentifierExtension
{
    /// <summary>Registers the claim types used to identify a user from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the claim types.</param>
    public static void AddClaimsIdentifier(this IServiceCollection services, Action<ClaimsIdentifierOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.Configure<ClaimsIdentifierOption>(options);
    }

    /// <summary>Registers the claim types used to identify a user from a configuration section (an array of claim types).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the array. The default is <c>Authentication:ClaimsIdentifier</c>. When the section is missing, the object identifier claim types (<c>ObjectIdentifier</c> and <c>OID</c> of <c>ClaimTypes</c>) are used.</param>
    public static void AddClaimsIdentifier(this IServiceCollection services, IConfiguration configuration, [DisallowNull] string sectionName = "Authentication:ClaimsIdentifier")
    {
        AddClaimsIdentifier(services, PrepareAction(configuration, sectionName));
    }

    /// <summary>Builds the action that fills a <see cref="ClaimsIdentifierOption"/> from a configuration section.</summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the array of claim types.</param>
    /// <returns>An action that adds the claim types of the section, or the default object identifier claim types when the section does not exist.</returns>
    public static Action<ClaimsIdentifierOption> PrepareAction(IConfiguration configuration, [DisallowNull] string sectionName)
    {
        if (string.IsNullOrEmpty(sectionName))
        {
            throw new ArgumentNullException(sectionName);
        }

        var section = configuration.GetSection(sectionName) as IConfigurationSection;

        // Standard values used to identify a user.
        var values = new ClaimsIdentifierOption();
        values.AddRange([ClaimTypes.ObjectIdentifier, ClaimTypes.OID]);

        if (section.Exists())
        {
            var option = configuration.GetSection(sectionName).Get<ClaimsIdentifierOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            values.Clear();
            values.AddRange(option);
        }

        void options(ClaimsIdentifierOption o)
        {
            o.AddRange(values);
        }

        return options;
    }
}
