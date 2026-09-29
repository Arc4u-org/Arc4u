using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Dapr;

/// <summary>Extension methods to register the options of a Dapr state store cache.</summary>
public static class DaprCacheExtension
{
    /// <summary>Registers the options of the Dapr cache with the given name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddDaprCache("Shared", o => o.Name = "statestore");
    /// </code>
    /// </example>
    public static IServiceCollection AddDaprCache(this IServiceCollection services, [DisallowNull] string name, Action<DaprCacheOption> options)
    {
        var rawCacheOption = new DaprCacheOption();
        new Action<DaprCacheOption>(options).Invoke(rawCacheOption);
        var action = new Action<DaprCacheOption>(o =>
        {
            o.Name = rawCacheOption.Name;
        });

        ArgumentException.ThrowIfNullOrEmpty(name);

        services.Configure<DaprCacheOption>(name, action);

        return services;
    }

    /// <summary>Registers the options of the Dapr cache with the given name from a configuration section. Nothing is registered when the section does not exist.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The path of the section that holds the <see cref="DaprCacheOption"/> values.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty and the section exists.</exception>
    public static IServiceCollection AddDaprCache(this IServiceCollection services, [DisallowNull] string name, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName)
    {
        var section = configuration.GetSection(sectionName);

        if (section.Exists())
        {
            var option = configuration.GetSection(sectionName).Get<DaprCacheOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            void options(DaprCacheOption o)
            {
                o.Name = option.Name;
            }

            services.AddDaprCache(name, options);
        }

        return services;
    }

}
