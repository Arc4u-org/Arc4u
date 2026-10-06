using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Redis;

/// <summary>Extension methods to register the options of a Redis Sentinel cache.</summary>
public static class RedisSentinelCacheExtension
{
    /// <summary>Registers the options of the Redis Sentinel cache with the given name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="options">The action that configures the options; the master name, at least one sentinel endpoint and the instance name are required.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/>, the master name or the instance name is <see langword="null"/> or empty, or the sentinel endpoints are empty.</exception>
    /// <exception cref="ArgumentNullException">The sentinel endpoints are <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddRedisSentinelCache("Shared", o =>
    /// {
    ///     o.InstanceName = "MyApp";
    ///     o.MasterName = "mymaster";
    ///     o.SentinelEndpoints = ["sentinel1:26379", "sentinel2:26379"];
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRedisSentinelCache(this IServiceCollection services, [DisallowNull] string name, Action<RedisSentinelCacheOption> options)
    {
        var validate = new RedisSentinelCacheOption();
        new Action<RedisSentinelCacheOption>(options).Invoke(validate);

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(validate.MasterName, nameof(validate.MasterName));
        ArgumentNullException.ThrowIfNull(validate.SentinelEndpoints, nameof(validate.SentinelEndpoints));
        if (validate.SentinelEndpoints.Length == 0)
        {
            throw new ArgumentException("At least one Sentinel endpoint must be provided in SentinelEndpoints.", nameof(options));
        }
        ArgumentException.ThrowIfNullOrEmpty(validate.InstanceName, nameof(validate.InstanceName));

        services.Configure<RedisSentinelCacheOption>(name, options);

        return services;
    }

    /// <summary>Registers the options of the Redis Sentinel cache with the given name from a configuration section. Nothing is registered when the section does not exist.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The path of the section that holds the <see cref="RedisSentinelCacheOption"/> values.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException">The section exists but the name, the master name or the instance name is <see langword="null"/> or empty, or its sentinel endpoints are empty.</exception>
    public static IServiceCollection AddRedisSentinelCache(this IServiceCollection services, [DisallowNull] string name, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName)
    {
        var section = configuration.GetSection(sectionName) as IConfigurationSection;

        if (section.Exists())
        {
            var option = section.Get<RedisSentinelCacheOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            void options(RedisSentinelCacheOption o)
            {
                o.InstanceName = option.InstanceName;
                o.MasterName = option.MasterName;
                o.SentinelEndpoints = option.SentinelEndpoints;
                o.RedisPassword = option.RedisPassword;
                o.DefaultDatabase = option.DefaultDatabase;
                o.SerializerName = option.SerializerName;
            }

            services.AddRedisSentinelCache(name, options);
        }

        return services;
    }
}
