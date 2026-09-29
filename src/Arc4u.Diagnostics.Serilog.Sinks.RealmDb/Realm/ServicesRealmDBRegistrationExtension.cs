using Microsoft.Extensions.DependencyInjection;
using Realms;
using Serilog;

namespace Arc4u.Diagnostics.Serilog.Sinks.RealmDb;

/// <summary>Extension methods to register the Realm log store.</summary>
public static class ServicesRealmDBRegistrationExtension
{
    /// <summary>Registers, as singletons, the Realm configuration created by <see cref="RealmDBExtension.DefaultConfig"/> and the <see cref="ILogStore"/> implemented by <see cref="RealmLoggingDbCtx"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// // Namespace: Arc4u.Diagnostics.Serilog.Sinks.RealmDb
    /// builder.Services.AddRealmDBLog();
    /// </code>
    /// </example>
    public static IServiceCollection AddRealmDBLog(this IServiceCollection services)
    {
        services.AddSingleton<RealmConfiguration>(RealmDBExtension.DefaultConfig());
        services.AddSingleton<ILogStore, RealmLoggingDbCtx>();

        return services;
    }
}
