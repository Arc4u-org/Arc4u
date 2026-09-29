using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Middleware;

/// <summary>Adds the <see cref="ValidateResourcesRightMiddleware"/>.</summary>
public static class ValidateResourcesRightMiddlewareExtension
{
    /// <summary>Adds the <see cref="ValidateResourcesRightMiddleware"/> to the request pipeline from code. The middleware is not added when no resource is configured.</summary>
    /// <param name="app">The application builder.</param>
    /// <param name="options">The action that configures the protected resources.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">A resource has no path or no policy, or the default content is empty.</exception>
    public static IApplicationBuilder UseResourcesRightValidationFor(this IApplicationBuilder app, Action<ValidateResourcesRightMiddlewareOptions> options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        var validate = new ValidateResourcesRightMiddlewareOptions();
        options(validate);

        string? configErrors = null;

        if (validate.ResourcesPolicies is null || validate.ResourcesPolicies.Count == 0)
        {
            return app; // do not use the middleware.
        }
        else
        {
            foreach (var option in validate.ResourcesPolicies)
            {
                if (string.IsNullOrWhiteSpace(option.Key))
                {
                    configErrors += "Key must be filled!" + System.Environment.NewLine;
                }

                if (string.IsNullOrWhiteSpace(option.Value.Path))
                {
                    configErrors += "Path must be filled!" + System.Environment.NewLine;
                }

                if (string.IsNullOrWhiteSpace(option.Value.AuthorizationPolicy))
                {
                    configErrors += "AuthorizationPolicy must be filled!" + System.Environment.NewLine;
                }

                if (string.IsNullOrWhiteSpace(option.Value.ContentToDisplay))
                {
                    option.Value.ContentToDisplay = validate.DefaultContent;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(validate.DefaultContent))
        {
            configErrors = "Default message content must be defined!" + System.Environment.NewLine;
        }

        if (configErrors is not null)
        {
            throw new ConfigurationException(configErrors);
        }

        return app.UseMiddleware<ValidateResourcesRightMiddleware>(validate);
    }

    /// <summary>Adds the <see cref="ValidateResourcesRightMiddleware"/> to the request pipeline from a configuration section.</summary>
    /// <param name="app">The application builder.</param>
    /// <param name="sectionName">The section, bound to <see cref="ValidateResourcesRightMiddlewareOptions"/>. The default is <c>Authentication:ResourcesRights</c>.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/> or <paramref name="sectionName"/> is empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// app.UseAuthentication();
    /// app.UseResourcesRightValidationFor();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseResourcesRightValidationFor(this IApplicationBuilder app, string sectionName = "Authentication:ResourcesRights")
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName));
        }

        var section = app.ApplicationServices.GetRequiredService<IConfiguration>().GetSection(sectionName);

        if (section is null && !section.Exists())
        {
            throw new ConfigurationException($"Section {sectionName} does not exist!");
        }

        app.UseResourcesRightValidationFor(options =>
        {
            var resources = section.Get<ValidateResourcesRightMiddlewareOptions>();

            if (resources is null)
            {
                return;
            }

            options.DefaultContent = resources.DefaultContent;
            options.ResourcesPolicies = resources.ResourcesPolicies;
        });

        return app;
    }
}

