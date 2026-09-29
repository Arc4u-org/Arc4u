using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the <see cref="ClaimsFillerOptions"/>.</summary>
public static class AddClaimsFillerExtension
{
    /// <summary>The claim types excluded from the principal when <see cref="ClaimsFillerOptions.ClaimsToExclude"/> is not configured.</summary>
    public static readonly List<string> DefaultClaimsToExclude = [ "aud", "iss", "iat", "nbf", "acr", "aio", "appidacr", "ipaddr", "scp", "tid", "uti", "unique_name", "apptype", "appid", "ver" ];
    /// <summary>Registers the <see cref="ClaimsFillerOptions"/> from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <exception cref="ConfigurationException"><see cref="ClaimsFillerOptions.ExpireClaim"/> is empty, or is part of <see cref="ClaimsFillerOptions.ClaimsToExclude"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddClaimsFiller(o =>
    /// {
    ///     o.LoadClaimsFromClaimsFillerProvider = true;
    ///     o.ExpireClaim = "exp";
    /// });
    /// </code>
    /// </example>
    public static void AddClaimsFiller(this IServiceCollection services, Action<ClaimsFillerOptions> options)
    {
        var validate = new ClaimsFillerOptions();
        options(validate);

        if (string.IsNullOrWhiteSpace(validate.ExpireClaim))
        {
            throw new ConfigurationException("Expire claim must be provided, usually 'exp'.");
        }

        if (validate.ClaimsToExclude.Any(k => k.Equals(validate.ExpireClaim, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConfigurationException($"The claim use to define when the validity period is expired: {validate.ExpireClaim}, cannot be excluded.");
        }

        services.Configure<ClaimsFillerOptions>(options);
    }

    /// <summary>
    /// Registers the <see cref="ClaimsFillerOptions"/> from a configuration section.
    /// When the section or its <c>ClaimsToExclude</c> child is missing, <see cref="DefaultClaimsToExclude"/> is used.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section to bind. The default is <c>Authentication:ClaimsMiddleWare:ClaimsFiller</c>.</param>
    /// <exception cref="ConfigurationException"><see cref="ClaimsFillerOptions.ExpireClaim"/> is empty, or is part of <see cref="ClaimsFillerOptions.ClaimsToExclude"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddClaimsFiller(configuration);
    /// </code>
    /// </example>
    public static void AddClaimsFiller(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication:ClaimsMiddleWare:ClaimsFiller")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Get the default values.
        var options = new ClaimsFillerOptions();

        if (!string.IsNullOrWhiteSpace(sectionName))
        {
            var section = configuration.GetSection(sectionName);

            if (section.Exists())
            {
                section.Bind(options);
                if (!section.GetSection("ClaimsToExclude").Exists())
                {
                    options!.ClaimsToExclude = DefaultClaimsToExclude;
                }
            }
            else
            {
                options.ClaimsToExclude = DefaultClaimsToExclude;
            }
        }

        AddClaimsFiller(services, o =>
        {
            o.LoadClaimsFromClaimsFillerProvider = options.LoadClaimsFromClaimsFillerProvider;
            o.ClaimsToExclude = options.ClaimsToExclude;
            o.ExpireClaim = options.ExpireClaim;
        });
    }
}
