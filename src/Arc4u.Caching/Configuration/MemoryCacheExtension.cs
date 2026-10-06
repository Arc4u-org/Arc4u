using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Arc4u.Configuration.Memory;
/// <summary>Extension methods to register the options of a memory cache.</summary>
public static class MemoryCacheExtension
{
    /// <summary>Registers the options of the memory cache with the given name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddMemoryCache("Volatile", o => o.SizeLimitInMB = 50);
    /// </code>
    /// </example>
    public static IServiceCollection AddMemoryCache(this IServiceCollection services, [DisallowNull] string name, Action<MemoryCacheOption> options)
    {
        var rawCacheOption = new MemoryCacheOption();
        new Action<MemoryCacheOption>(options).Invoke(rawCacheOption);
        var action = new Action<MemoryCacheOption>(o =>
        {
            o.CompactionPercentage = rawCacheOption.CompactionPercentage;
            o.SizeLimitInMB = rawCacheOption.SizeLimitInMB;
            o.SerializerName = rawCacheOption.SerializerName;
        });

        ArgumentException.ThrowIfNullOrEmpty(name);

        services.Configure<MemoryCacheOption>(name, action);

        return services;
    }

    /// <summary>Registers the options of the memory cache with the given name from a configuration section. Nothing is registered when the section does not exist.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The path of the section that holds the <see cref="MemoryCacheOption"/> values.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty and the section exists.</exception>
    public static IServiceCollection AddMemoryCache(this IServiceCollection services, [DisallowNull] string name, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName)
    {
        var section = configuration.GetSection(sectionName) as IConfigurationSection;

        if (section.Exists())
        {
            var option = configuration.GetSection(sectionName).Get<MemoryCacheOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            void options(MemoryCacheOption o)
            {
                o.SerializerName = option.SerializerName;
                o.SizeLimitInMB = option.SizeLimitInMB;
                o.CompactionPercentage = option.CompactionPercentage;
            }

            services.AddMemoryCache(name, options);
        }

        return services;
    }

}
