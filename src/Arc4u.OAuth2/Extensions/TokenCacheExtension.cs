using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the <see cref="TokenCacheOptions"/>.</summary>
public static class TokenCacheExtension
{
    /// <summary>Registers the token cache options from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <exception cref="ConfigurationException"><see cref="TokenCacheOptions.CacheName"/> is empty or <see cref="TokenCacheOptions.MaxTime"/> is not greater than zero.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddTokenCache(o =>
    /// {
    ///     o.CacheName = "Token";
    ///     o.MaxTime = TimeSpan.FromMinutes(30);
    /// });
    /// </code>
    /// </example>
    public static void AddTokenCache(this IServiceCollection services, Action<TokenCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var tokenCacheOptions = new TokenCacheOptions();
        options(tokenCacheOptions);

        AddTokenCache(services, tokenCacheOptions);

    }
    /// <summary>Registers the token cache options from a configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section to bind. The default is <c>Authentication:TokenCache</c>. <c>MaxTime</c> keeps its default (50 minutes) when it is not in the section.</param>
    /// <exception cref="ConfigurationException"><see cref="TokenCacheOptions.CacheName"/> is empty or <see cref="TokenCacheOptions.MaxTime"/> is not greater than zero.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddTokenCache(configuration);
    /// </code>
    /// </example>
    public static void AddTokenCache(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication:TokenCache")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sectionName);

        var tokenCacheOptions = new TokenCacheOptions();
        var defaultMaxTime = tokenCacheOptions.MaxTime;

        var section = configuration.GetSection(sectionName);
        if (section.Exists())
        {
            tokenCacheOptions = section.Get<TokenCacheOptions>() ?? tokenCacheOptions;

            if (!section.GetChildren().Any(c => c.Key == nameof(TokenCacheOptions.MaxTime)))
            {
                tokenCacheOptions.MaxTime = defaultMaxTime;
            }

        }

        AddTokenCache(services, tokenCacheOptions);
    }

    /// <summary>
    /// Registers named <see cref="TokenCacheOptions"/>. A value that is not given is taken, when the options are created, from the <paramref name="fallbackName"/> options,
    /// or from the default <see cref="TokenCacheOptions"/> when it is <see langword="null"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options, passed as <c>optionsName</c> to the <c>ITokenCache</c>.</param>
    /// <param name="cacheName">The name of the cache, or <see langword="null"/> to take the one of the fallback options.</param>
    /// <param name="maxTime">The time after which a cached token is evicted, or <see langword="null"/> to take the one of the fallback options.</param>
    /// <param name="fallbackName">The name of the options giving the values that are not given, or <see langword="null"/> for the default options.</param>
    /// <exception cref="ConfigurationException"><paramref name="maxTime"/> is not greater than zero.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddNamedTokenCache("DownstreamApi", cacheName: "Redis", maxTime: TimeSpan.FromMinutes(10));
    /// </code>
    /// </example>
    public static void AddNamedTokenCache(this IServiceCollection services, string name, string? cacheName, TimeSpan? maxTime, string? fallbackName = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (maxTime is not null && maxTime <= TimeSpan.Zero)
        {
            throw new ConfigurationException($"[{name}] TokenCache.MaxTime must be greater than zero.");
        }

        cacheName = string.IsNullOrWhiteSpace(cacheName) ? null : cacheName;

        // The fallback options are read through the service provider when the named options are created:
        // depending on IOptionsMonitor<TokenCacheOptions> directly would be a circular dependency of the options factory.
        services.AddOptions<TokenCacheOptions>(name)
                .Configure<IServiceProvider>((named, serviceProvider) =>
                {
                    var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();
                    var fallback = fallbackName is null ? monitor.CurrentValue : monitor.Get(fallbackName);

                    named.CacheName = cacheName ?? fallback.CacheName;
                    named.MaxTime = maxTime ?? fallback.MaxTime;
                });
    }

    /// <summary>
    /// Registers named <see cref="TokenCacheOptions"/> from a configuration section holding <c>CacheName</c> and <c>MaxTime</c>. A value that is not in the section is taken
    /// from the <paramref name="fallbackName"/> options, or from the default <see cref="TokenCacheOptions"/> when it is <see langword="null"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options, passed as <c>optionsName</c> to the <c>ITokenCache</c>.</param>
    /// <param name="section">The section to read.</param>
    /// <param name="fallbackName">The name of the options giving the values that are not in the section, or <see langword="null"/> for the default options.</param>
    /// <exception cref="ConfigurationException">The <c>MaxTime</c> of the section is not greater than zero.</exception>
    public static void AddNamedTokenCache(this IServiceCollection services, string name, IConfigurationSection section, string? fallbackName = null)
    {
        ArgumentNullException.ThrowIfNull(section);

        // MaxTime has a default value, so the binder cannot tell if it was given: look at the section.
        var bound = section.Get<TokenCacheOptions>() ?? new TokenCacheOptions();
        TimeSpan? maxTime = section.GetSection(nameof(TokenCacheOptions.MaxTime)).Exists() ? bound.MaxTime : null;

        services.AddNamedTokenCache(name, bound.CacheName, maxTime, fallbackName);
    }

    /// <summary>
    /// Registers the <see cref="TokenCacheOptions"/> shared by the entries of a section (for example <c>Authentication:ClientTokens</c>) from the
    /// <paramref name="sectionName"/> section, as named options <paramref name="name"/>. A value that is not in the section is taken from the default <see cref="TokenCacheOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the shared <c>CacheName</c> and <c>MaxTime</c>, for example <c>Authentication:ClientTokensCache</c>; empty for none.</param>
    /// <param name="name">The name of the shared options, reserved: no entry can have it.</param>
    /// <param name="entryKeys">The keys of the entries that use the shared options.</param>
    /// <returns>
    /// <paramref name="name"/>, or <see langword="null"/> when <paramref name="sectionName"/> is empty or the section does not exist (the entries then use the default options).
    /// </returns>
    /// <exception cref="ConfigurationException">An entry is named <paramref name="name"/> (ignoring case), or the <c>MaxTime</c> of the section is not greater than zero.</exception>
    public static string? AddSharedTokenCache(this IServiceCollection services, IConfiguration configuration, string sectionName, string name, IEnumerable<string> entryKeys)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(entryKeys);

        // An empty section name turns the shared section off, as a missing section does.
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            return null;
        }

        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            return null;
        }

        // The shared options are registered under this name: an entry with the same name would share its registration.
        if (entryKeys.Any(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConfigurationException($"[{name}] An entry cannot be named '{name}' when the '{sectionName}' section exists.");
        }

        services.AddNamedTokenCache(name, section);
        return name;
    }

    private static void AddTokenCache(IServiceCollection services, TokenCacheOptions tokenCacheOptions)
    {
        if (tokenCacheOptions is null)
        {
            throw new ConfigurationException("TokenCacheOptions is not defined in the configuration file.");
        }

        if (string.IsNullOrWhiteSpace(tokenCacheOptions.CacheName))
        {
            throw new ConfigurationException("TokenCacheOptions.CacheName is not defined in the configuration file.");
        }

        // A cache entry needs a positive lifetime, as for the named options.
        if (tokenCacheOptions.MaxTime <= TimeSpan.Zero)
        {
            throw new ConfigurationException("TokenCacheOptions.MaxTime is not defined in the configuration file, or is not greater than zero.");
        }

        services.Configure<TokenCacheOptions>(options =>
        {
            options.CacheName = tokenCacheOptions.CacheName;
            options.MaxTime = tokenCacheOptions.MaxTime;
        });

        services.AddTokenCacheResolver();
    }

    /// <summary>
    /// Registers the <see cref="TokenCacheResolver"/>, once, as a singleton: it resolves the cache of the default or named <see cref="TokenCacheOptions"/> for
    /// <see cref="ApplicationCache"/> and the token providers. <c>AddTokenCache</c>, <c>AddJwtAuthentication</c>, <c>AddClientTokens</c>, <c>AddOnBehalfOf</c>,
    /// <c>AddOnBehalfOfSettings</c> and <c>AddBasicAuthenticationSettings</c> call it; call it yourself when you register <see cref="ApplicationCache"/> without them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddTokenCacheResolver(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A factory, not a type: the container validation (ValidateOnBuild, on by default in Development) does not require the ICacheHelper of an application
        // that registers no token cache (an API that only validates inbound tokens); the dependencies are resolved when the resolver is first used.
        services.TryAddSingleton(serviceProvider => new TokenCacheResolver(serviceProvider.GetRequiredService<ICacheHelper>(),
                                                                          serviceProvider.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>(),
                                                                          serviceProvider.GetServices<IConfigureOptions<TokenCacheOptions>>(),
                                                                          serviceProvider.GetRequiredService<ILogger<TokenCacheResolver>>()));
    }
}
