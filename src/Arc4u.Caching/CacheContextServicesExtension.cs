using Arc4u.Configuration.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Arc4u.Configuration.Redis;
using Arc4u.Configuration.Sql;
using Arc4u.Configuration.Dapr;

namespace Arc4u.Caching;

/// <summary>Extension methods to register the caches described in the <c>Caching</c> configuration section.</summary>
public static class CacheContextServicesExtension
{
    /// <summary>
    /// Registers the <see cref="ICacheContext"/> and the options of every cache declared in the configuration section. For the cache at position <c>n</c> of the <c>Caches</c> array,
    /// the options are read from <c>{sectionName}:Caches:n:Settings</c> and registered according to its <c>Kind</c> (<c>Memory</c>, <c>Redis</c>, <c>RedisSentinel</c>, <c>Sql</c> or <c>Dapr</c>, case-insensitive; other kinds are ignored).
    /// The <see cref="ICache"/> implementations themselves are provided by the <c>Arc4u.Caching.*</c> packages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The name of the configuration section. Default is <c>Caching</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The section does not exist or cannot be bound.</exception>
    /// <example>
    /// With a configuration such as:
    /// <code language="json">
    /// {
    ///   "Caching": {
    ///     "Default": "Volatile",
    ///     "Caches": [
    ///       { "Name": "Volatile", "Kind": "Memory", "IsAutoStart": true, "Settings": { "SizeLimitInMB": 100 } }
    ///     ]
    ///   }
    /// }
    /// </code>
    /// the caches are registered in Program.cs with:
    /// <code language="csharp">
    /// builder.Services.AddCacheContext(builder.Configuration);
    /// </code>
    /// </example>
    public static void AddCacheContext(this IServiceCollection services, IConfiguration configuration, string sectionName = "Caching")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetRequiredSection(sectionName);

        var config = section.Get<Configuration.Caching>() ?? throw new InvalidOperationException("Configuration for caching is missing.");

        services.TryAddSingleton<ICacheContext, CacheContext>();

        for (var idx = 0; idx < config.Caches.Count; idx++)
        {
            var cache = config.Caches[idx];

            switch (cache.Kind.ToLowerInvariant())
            {
                case "memory":
                    services.AddMemoryCache(cache.Name, configuration, BuildCacheSettingsSectionPath(idx, sectionName));
                    break;
                case "redis":
                    services.AddRedisCache(cache.Name, configuration, BuildCacheSettingsSectionPath(idx, sectionName));
                    break;
                case "redissentinel":
                    services.AddRedisSentinelCache(cache.Name, configuration, BuildCacheSettingsSectionPath(idx, sectionName));
                    break;
                case "sql":
                    services.AddSqlCache(cache.Name, configuration, BuildCacheSettingsSectionPath(idx, sectionName));
                    break;
                case "dapr":
                    services.AddDaprCache(cache.Name, configuration, BuildCacheSettingsSectionPath(idx, sectionName));
                    break;
            }
        }

    }

    private static string BuildCacheSettingsSectionPath(int idx, string rootSectionName)
    {
        return $"{rootSectionName}:Caches:{idx}:Settings";
    }

    /// <summary>Gets the registered <see cref="ICacheContext"/>.</summary>
    /// <param name="container">The service provider.</param>
    /// <returns>The <see cref="ICacheContext"/>, or <see langword="null"/> when it is not registered.</returns>
    public static ICacheContext? GetCacheContext(this IServiceProvider container)
    {
        return container.GetService<ICacheContext>();
    }
}
