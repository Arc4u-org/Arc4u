using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Middleware;
/// <summary>Registers and adds the <see cref="ForceOpenIdMiddleWare"/>.</summary>
public static class ForceOpenIdMiddleWareOptionsExtension
{
    /// <summary>Registers the <see cref="ForceOpenIdMiddleWareOptions"/> from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action that configures the options.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddForceOpenId(options =>
    /// {
    ///     options.ForceAuthenticationForPaths.Add("/swagger*");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddForceOpenId(this IServiceCollection services, Action<ForceOpenIdMiddleWareOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services;
    }

    /// <summary>Registers the <see cref="ForceOpenIdMiddleWareOptions"/> from a configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section to bind. The default is <c>Authentication:ClaimsMiddleWare:ForceOpenId</c>.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddForceOpenId(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddForceOpenId(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication:ClaimsMiddleWare:ForceOpenId")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ForceOpenIdMiddleWareOptions>(configuration.GetSection(sectionName));
        return services;
    }

    /// <summary>Adds the <see cref="ForceOpenIdMiddleWare"/> to the request pipeline. Call it after the authentication middleware.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseForceOpenId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<ForceOpenIdMiddleWare>(); // Now uses IOptions<T>
    }
}
