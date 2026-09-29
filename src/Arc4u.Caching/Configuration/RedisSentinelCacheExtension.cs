using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Redis;

/// <summary>Extension methods to register the options of a Redis Sentinel cache.</summary>
public static class RedisSentinelCacheExtension
{
    /// <summary>
    /// Register Redis Sentinel cache options via code
    /// </summary>
    public static IServiceCollection AddRedisSentinelCache(this IServiceCollection services, [DisallowNull] string name, Action<RedisSentinelCacheOption> options)
    {
        var validate = new RedisSentinelCacheOption();
        new Action<RedisSentinelCacheOption>(options).Invoke(validate);

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(validate.MasterName, nameof(validate.MasterName));
        ArgumentNullException.ThrowIfNull(validate.SentinelEndpoints, nameof(validate.SentinelEndpoints));
        if (validate.SentinelEndpoints.Length == 0)
        {
            throw new ArgumentNullException("At least one Sentinel endpoint must be provided.", nameof(validate.SentinelEndpoints));
        }
        ArgumentException.ThrowIfNullOrEmpty(validate.InstanceName, nameof(validate.InstanceName));

        services.Configure<RedisSentinelCacheOption>(name, options);

        return services;
    }

    /// <summary>
    /// Register Redis Sentinel cache options via IConfiguration section
    /// </summary>
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
