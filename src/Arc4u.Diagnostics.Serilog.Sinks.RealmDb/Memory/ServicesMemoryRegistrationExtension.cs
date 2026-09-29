using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>Extension methods to register the in-memory log store.</summary>
public static class ServicesMemoryRegistrationExtension
{
    /// <summary>Registers the in-memory <see cref="MemoryLogMessages"/> and <see cref="ILogStore"/> (implemented by <see cref="MemoryLogStore"/>) as singletons.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// // Namespace: Arc4u.Diagnostics.Serilog.Sinks.Memory
/// builder.Services.AddMemoryLogDB();
    /// </code>
    /// </example>
    public static IServiceCollection AddMemoryLogDB(this IServiceCollection services)
    {
        services.AddSingleton<MemoryLogMessages>(MemoryLogDbSink.LogMessages);
        services.AddSingleton<ILogStore, MemoryLogStore>();

        return services;
    }
}
