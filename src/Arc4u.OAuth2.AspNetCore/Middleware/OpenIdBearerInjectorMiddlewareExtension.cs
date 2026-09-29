using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Middleware;

/// <summary>Registers and adds the <see cref="OpenIdBearerInjectorMiddleware"/>.</summary>
public static class OpenIdBearerInjectorMiddlewareExtension
{
    /// <summary>Registers the <see cref="OpenIdBearerInjectorOptions"/> from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">The on-behalf-of settings key or the provider key is empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddOpenIdBearerInjector(options =>
    /// {
    ///     options.OboProviderKey = "Obo";
    /// });
    /// </code>
    /// </example>
    public static void AddOpenIdBearerInjector(this IServiceCollection services, Action<OpenIdBearerInjectorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var validate = new OpenIdBearerInjectorOptions();
        options(validate);

        string? configErrors = null;
        if (string.IsNullOrEmpty(validate.OnBehalfOfOpenIdSettingsKey))
        {
            configErrors += "The on behalf of settings key must be defined." + System.Environment.NewLine;
        }

        if (string.IsNullOrEmpty(validate.OboProviderKey))
        {
            configErrors += "The token provider key to handle the on behalf of scenario must be defined." + System.Environment.NewLine;
        }

        if (configErrors is not null)
        {
            throw new ConfigurationException(configErrors);
        }

        services.Configure<OpenIdBearerInjectorOptions>(options);
        // Let a customer change the behavior by injecting his/her implementation.
        services.TryAddSingleton<IPostConfigureOptions<OpenIdBearerInjectorSettingsOptions>, PostConfigureOpenIdBearerInjectorSettings>();
    }

    /// <summary>Registers the <see cref="OpenIdBearerInjectorOptions"/> with their default values.</summary>
    /// <param name="services">The service collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddOpenIdBearerInjector();
    /// </code>
    /// </example>
    public static void AddOpenIdBearerInjector(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var validate = new OpenIdBearerInjectorOptions();

        services.AddOpenIdBearerInjector(options =>
        {
            options.OboProviderKey = validate.OboProviderKey;
            options.OnBehalfOfOpenIdSettingsKey = validate.OnBehalfOfOpenIdSettingsKey;
            options.OpenIdSettingsKey = validate.OpenIdSettingsKey;
        });

    }
    /// <summary>Adds the <see cref="OpenIdBearerInjectorMiddleware"/> to the request pipeline. Call it after the authentication middleware.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseOpenIdBearerInjector([DisallowNull] this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<OpenIdBearerInjectorMiddleware>();
    }
}
