using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.Caching.Memory;

/// <summary>Extension methods to register the <see cref="ICache"/> implementation of the <c>Arc4u.Caching.Memory</c> package.</summary>
public static class MemoryCacheServicesExtension
{
    /// <summary>
    /// Registers <see cref="MemoryCache"/> with the kind <see cref="CacheContext.Memory"/> as keyed <see cref="ICache"/> service, so that the caches declared with this <c>Kind</c> in the caching configuration section can be resolved by the <see cref="ICacheContext"/>.
    /// Calling it more than once has no effect.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddMemoryCacheKind(this IServiceCollection services)
    {
        services.TryAddKeyedTransient<ICache, MemoryCache>(CacheContext.Memory);

        return services;
    }
}
