using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Arc4u.Results.Logging;

/// <summary>
/// Registers <see cref="FluentLogger"/> as the logger used by FluentResults (<c>LogIfFailed</c>, <c>Log</c>, ...).
/// </summary>
public static class ResultLoggerServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FluentLogger"/> as the <see cref="IResultLogger"/> (singleton, unless one is already registered) and a hosted service
    /// that gives it to FluentResults with <c>Result.Setup</c> when the host starts. That hosted service is registered first, so it runs
    /// before the other hosted services (unless <c>HostOptions.ServicesStartConcurrently</c> is set).
    /// </summary>
    /// <remarks>
    /// <see cref="FluentLogger"/> needs the Arc4u <c>ILogger&lt;T&gt;</c>: also call <c>AddApplicationContext()</c> (or <c>AddILogger()</c>).
    /// Results logged before the host starts (code between <c>Build()</c> and <c>Run()</c>) or without a host are not logged:
    /// call <see cref="UseResultLogger"/> right after the service provider is built.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddApplicationContext();
    /// builder.Services.AddResultLogger();
    /// </code>
    /// </example>
    public static IServiceCollection AddResultLogger(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IResultLogger, FluentLogger>();

        // First in the collection: the host starts hosted services in registration order, so the
        // logger is set before any other hosted service can log a result.
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(ResultLoggerSetup)))
        {
            services.Insert(0, ServiceDescriptor.Singleton<IHostedService, ResultLoggerSetup>());
        }

        return services;
    }

    /// <summary>
    /// Gives the registered <see cref="IResultLogger"/> to FluentResults with <c>Result.Setup</c>.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The same <paramref name="serviceProvider"/>.</returns>
    public static IServiceProvider UseResultLogger(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var logger = serviceProvider.GetRequiredService<IResultLogger>();
        Result.Setup(cfg => cfg.Logger = logger);

        return serviceProvider;
    }

    private sealed class ResultLoggerSetup(IServiceProvider serviceProvider) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            serviceProvider.UseResultLogger();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
