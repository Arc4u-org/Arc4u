using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.Caching.Redis;

/// <summary>Extension methods to register the <see cref="ICache"/> implementations of the <c>Arc4u.Caching.Redis</c> package.</summary>
public static class RedisCacheServicesExtension
{
    /// <summary>
    /// Registers <see cref="RedisCache"/> and <see cref="RedisSentinelCache"/> with the kinds <see cref="CacheContext.Redis"/> and <see cref="CacheContext.RedisSentinel"/> as keyed <see cref="ICache"/> services, so that the caches declared with this <c>Kind</c> in the caching configuration section can be resolved by the <see cref="ICacheContext"/>.
    /// Calling it more than once has no effect.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddRedisCacheKinds(this IServiceCollection services)
    {
        services.TryAddKeyedTransient<ICache, RedisCache>(CacheContext.Redis);
        services.TryAddKeyedTransient<ICache, RedisSentinelCache>(CacheContext.RedisSentinel);

        return services;
    }
}
