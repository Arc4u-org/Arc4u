using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Arc4u.Dependency;

/// <summary>Extension methods to register the Arc4u logging on an <see cref="IServiceCollection"/>.</summary>
public static class ServicesRegistrationExtension
{
    /// <summary>
    /// Add to the <see cref="IServiceCollection"/> a scoped instance of <see cref="ILogger{LoggerMessage}"/> as ILogger.
    /// This allows for static classes to resolve based on ILogger and via the fluent API, the from() method replaces
    /// the class name.
    /// </summary>
    /// <param name="services"></param>
    /// <returns><see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddILogger(this IServiceCollection services)
    {
        services.RemoveAll(typeof(ILogger<>));

        services.TryAddKeyedScoped<IAddPropertiesToLog, NullLoggerProperties>("Scoped");
        services.TryAddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        // Add the Arc4u Logger<T> implementation.
        services.TryAddScoped(typeof(IScopedLogger<>), typeof(ScopedLoggerWrapper<>));
        services.TryAddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));

        services.AddTransient<ILogger>((serviceProvider) => serviceProvider.GetRequiredService<ILogger<DefaultLogger>>());

        return services;
    }
}
