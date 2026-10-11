using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.OAuth2.AspNetCore.Diagnostics;

/// <summary>Extension methods to add the identity of the current HTTP request to the entries of the Arc4u <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/>.</summary>
public static class HttpContextLoggingPropertiesExtension
{
    /// <summary>
    /// Registers <see cref="HttpContextLoggingProperties"/> as <see cref="IAddPropertiesToLog"/> under the key <c>"Transient"</c>, used by the loggers resolved
    /// from the root (the loggers of singletons), and the <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> it needs.
    /// It does nothing when a provider is already registered under that key: <c>AddILogger()</c> (called by <c>AddApplicationContext()</c>) registers a
    /// <see cref="NullLoggerProperties"/> there, so call it before them.
    /// The Arc4u authentication extensions (<c>AddJwtAuthentication</c>, <c>AddOidcAuthentication</c>, <c>AddHybridAuthentication</c>) also call it, which only
    /// has an effect when they are added before <c>AddApplicationContext()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddHttpContextLoggingProperties();
    /// builder.Services.AddApplicationContext();
    /// </code>
    /// </example>
    public static IServiceCollection AddHttpContextLoggingProperties(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        services.TryAddKeyedTransient<IAddPropertiesToLog, HttpContextLoggingProperties>("Transient");

        return services;
    }
}
