using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.Configuration.Store;

using Internals;

/// <summary>
/// Extension methods to register the service that keeps the configuration in sync with the section store.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(15);

    private static SectionStoreMonitor CreateSectionStoreMonitor(IServiceProvider serviceProvider, TimeSpan pollingInterval)
    {
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<SectionStoreMonitor>>();
        return new SectionStoreMonitor(pollingInterval, serviceScopeFactory, logger);
    }

    /// <summary>
    /// Add the monitoring service that will track the changes in the store
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pollingInterval">The delay between two checks of the store for modified sections.</param>
    /// <returns>The service collection, to chain calls.</returns>
    public static IServiceCollection AddSectionStoreService(this IServiceCollection services, TimeSpan pollingInterval)
    {
        services.AddSingleton<ISectionStoreService, SectionStoreService>();
        services.AddHostedService(serviceProvider => CreateSectionStoreMonitor(serviceProvider, pollingInterval));
        return services;
    }

    /// <summary>
    /// Add the monitoring service that will track the changes in the store with a default polling interval (15 seconds).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    public static IServiceCollection AddSectionStoreService(this IServiceCollection services) => services.AddSectionStoreService(DefaultPollingInterval);
}
