using Arc4u.Diagnostics.Monitoring;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods to register the process monitoring.</summary>
public static class SystemResourcesExtension
{
    /// <summary>Registers the <see cref="Arc4u.Diagnostics.Monitoring.SystemResources"/> hosted service, which periodically logs the CPU and memory usage of the process in the Monitoring category.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// // AddILogger is defined in the Arc4u.Dependency namespace.
    /// builder.Services.AddILogger()
    ///                 .AddSystemMonitoring();
    /// </code>
    /// </example>
    public static IServiceCollection AddSystemMonitoring(this IServiceCollection services)
    {
        services.AddHostedService<SystemResources>();

        return services;
    }
}
