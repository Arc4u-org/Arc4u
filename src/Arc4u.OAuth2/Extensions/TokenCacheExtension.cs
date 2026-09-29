using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the <see cref="TokenCacheOptions"/>.</summary>
public static class TokenCacheExtension
{
    /// <summary>Registers the token cache options from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <exception cref="ConfigurationException"><see cref="TokenCacheOptions.CacheName"/> is empty or <see cref="TokenCacheOptions.MaxTime"/> is zero.</exception>
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
    /// <exception cref="ConfigurationException"><see cref="TokenCacheOptions.CacheName"/> is empty or <see cref="TokenCacheOptions.MaxTime"/> is zero.</exception>
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

        if (TimeSpan.Zero == tokenCacheOptions.MaxTime)
        {
            throw new ConfigurationException("TokenCacheOptions.MaxTime is not defined in the configuration file.");
        }

        services.Configure<TokenCacheOptions>(options =>
        {
            options.CacheName = tokenCacheOptions.CacheName;
            options.MaxTime = tokenCacheOptions.MaxTime;
        });
    }
}
