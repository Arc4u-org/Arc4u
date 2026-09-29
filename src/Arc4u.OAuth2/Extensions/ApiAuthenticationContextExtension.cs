using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Extensions;

/// <summary>Registers the <see cref="ApiExtraContextAuthenticationOption"/>.</summary>
public static class ApiAuthenticationContextExtension
{
    /// <summary>Registers the <see cref="ApiExtraContextAuthenticationOption"/> from code. Nothing is done when a configuration for this options type is already registered.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="option">The action that configures the options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="option"/> is <see langword="null"/>.</exception>
    public static void AddAuthenticationApiContext(this IServiceCollection services, Action<ApiExtraContextAuthenticationOption> option )
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(services);

        // Check if any configuration for this options type exists
        var hasConfiguration = services.Any(x =>
            x.ServiceType == typeof(IConfigureOptions<ApiExtraContextAuthenticationOption>) ||
            x.ServiceType == typeof(IPostConfigureOptions<ApiExtraContextAuthenticationOption>) ||
            x.ServiceType == typeof(IValidateOptions<ApiExtraContextAuthenticationOption>));

        if (!hasConfiguration)
        {
            services.Configure(option);
        }

    }

    /// <summary>Registers the <see cref="ApiExtraContextAuthenticationOption"/> from configuration sections.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="section">The paths of the sections holding the extra parameters. When <see langword="null"/>, the defaults of <see cref="ApiExtraContextAuthenticationSectionOption"/> are used.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static void AddAuthenticationApiContext(this IServiceCollection services, IConfiguration configuration, ApiExtraContextAuthenticationSectionOption? section = null )
    {
        section ??= new ApiExtraContextAuthenticationSectionOption();

        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(services);

        void ApiExtraContextConfigurationFiller(ApiExtraContextAuthenticationOption option)
        {
            option.AuthorizationParameters = new CustomApiContextParameters(section.AuthorizationEndpointSectionPath, configuration);
            option.TokenParameters = new CustomApiContextParameters(section.TokenEndpointSectionPath, configuration);
        }

        AddAuthenticationApiContext(services, ApiExtraContextConfigurationFiller);
    }

    private class CustomApiContextParameters(string sectionName, IConfiguration configuration)
        : KeyValueSettings(sectionName, configuration);
}
