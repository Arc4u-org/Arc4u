using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.Caching.Sql;

/// <summary>Extension methods to register the <see cref="ICache"/> implementation of the <c>Arc4u.Caching.Sql</c> package.</summary>
public static class SqlCacheServicesExtension
{
    /// <summary>
    /// Registers <see cref="SqlCache"/> with the kind <see cref="CacheContext.Sql"/> as keyed <see cref="ICache"/> service, so that the caches declared with this <c>Kind</c> in the caching configuration section can be resolved by the <see cref="ICacheContext"/>.
    /// Calling it more than once has no effect.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddSqlCacheKind(this IServiceCollection services)
    {
        services.TryAddKeyedTransient<ICache, SqlCache>(CacheContext.Sql);

        return services;
    }
}
