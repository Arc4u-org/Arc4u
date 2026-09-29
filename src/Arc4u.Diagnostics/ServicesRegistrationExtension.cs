using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Arc4u.Dependency;

/// <summary>Extension methods to register the Arc4u logging on an <see cref="IServiceCollection"/>.</summary>
public static class ServicesRegistrationExtension
{
    /// <summary>
    /// Registers the Arc4u logging: <see cref="ILogger{TCategoryName}"/> is replaced by <see cref="LoggerWrapper{T}"/> (transient),
    /// the non-generic <see cref="ILogger"/> resolves to <c>ILogger&lt;DefaultLogger&gt;</c> (transient), and <see cref="IScopedLogger{T}"/> resolves to
    /// <see cref="ScopedLoggerWrapper{T}"/> (scoped). Any existing <see cref="ILogger{TCategoryName}"/> registration is removed first.
    /// Unless already registered, a <see cref="NullLoggerProperties"/> is registered as <see cref="IAddPropertiesToLog"/> under the keys <c>"Scoped"</c> and <c>"Transient"</c>.
    /// The registered loggers expose the fluent API of <see cref="ILoggerExtensions"/> (<c>Technical()</c>, <c>Business()</c>, <c>Monitoring()</c>), which also works on the
    /// non-generic <see cref="ILogger"/> so that static classes can log by passing the type with <c>Technical(Type)</c> or <c>Technical&lt;T&gt;()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddILogger();
    /// </code>
    /// </example>
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
