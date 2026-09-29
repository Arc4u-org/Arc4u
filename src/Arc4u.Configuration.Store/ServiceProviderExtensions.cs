using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Store;

using Internals;

/// <summary>
/// Extension methods to start the section store configuration once the service provider is built.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// This needs to be called AFTER the settings store (e.g. the Database Context) is fully defined.
    /// It creates, in the store, the sections declared with <c>AddSectionStoreConfiguration</c> that do not exist yet
    /// and enables the reloading of the configuration when the store changes.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The same service provider, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// var app = builder.Build();
    /// app.Services.UseSectionStoreConfiguration();
    /// </code>
    /// </example>
    public static IServiceProvider UseSectionStoreConfiguration(this IServiceProvider serviceProvider)
    {
        // the factory is a singleton, which is what we want/
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var sectionStoreService = serviceProvider.GetRequiredService<ISectionStoreService>();
        sectionStoreService.Startup(serviceScopeFactory);
        return serviceProvider;
    }
}
