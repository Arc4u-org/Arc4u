using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Arc4u.Security.Principal;

/// <summary>
/// Extension methods to register the <see cref="IApplicationContext"/>.
/// </summary>
public static class ApplicationContextServiceCollectionExtension
{
    /// <summary>
    /// Add the default logger and properties contexted with the scoped instance application context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// services.AddApplicationContext();
    /// </code>
    /// </example>
    public static IServiceCollection AddApplicationContext(this IServiceCollection services)
    {
        services.TryAddScoped<IAddPropertiesToLog, DefaultLoggingProperties>();

        // register the logger infrastructure as Scoped.
        services.AddILogger();
        services.TryAddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.TryAddSingleton<IActivitySourceFactory, DefaultActivitySourceFactory>();

        return services;
    }
}
