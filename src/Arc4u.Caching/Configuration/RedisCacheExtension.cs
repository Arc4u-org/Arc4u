using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Redis;
/// <summary>Extension methods to register the options of a Redis cache.</summary>
public static class RedisCacheExtension
{
    /// <summary>Registers the options of the Redis cache with the given name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="options">The action that configures the options; <see cref="RedisCacheOption.ConnectionString"/> and <see cref="RedisCacheOption.InstanceName"/> are required.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/>, the connection string or the instance name is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddRedisCache("Shared", o => o.ConnectionString = "localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, [DisallowNull] string name, Action<RedisCacheOption> options)
    {
        var validate = new RedisCacheOption();
        new Action<RedisCacheOption>(options).Invoke(validate);

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(validate.ConnectionString, nameof(validate.ConnectionString));
        ArgumentException.ThrowIfNullOrEmpty(validate.InstanceName, nameof(validate.InstanceName));

        services.Configure<RedisCacheOption>(name, options);

        return services;
    }

    /// <summary>Registers the options of the Redis cache with the given name from a configuration section. Nothing is registered when the section does not exist.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The path of the section that holds the <see cref="RedisCacheOption"/> values.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> or the connection string is <see langword="null"/> or empty and the section exists.</exception>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, [DisallowNull] string name, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName)
    {
        var section = configuration.GetSection(sectionName) as IConfigurationSection;

        if (section.Exists())
        {
            var option = configuration.GetSection(sectionName).Get<RedisCacheOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            void options(RedisCacheOption o)
            {
                o.SerializerName = option.SerializerName;
                o.ConnectionString = option.ConnectionString;
                o.InstanceName = option.InstanceName;
            }

            services.AddRedisCache(name, options);
        }

        return services;
    }

}
