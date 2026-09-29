using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

namespace Arc4u.OAuth2.Middleware;

/// <summary>Adds the <see cref="LogMonitoringTimeElapsedMiddleware"/>.</summary>
public static class LogMonitoringTimeElapsedMiddlewareExtension
{
    // ✅ AOT-compatible
    /// <summary>Adds the <see cref="LogMonitoringTimeElapsedMiddleware"/> to the request pipeline. An <c>ILogger</c> must be registered in the service collection.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// app.AddMonitoringTimeElapsed();
    /// </code>
    /// </example>
    public static IApplicationBuilder AddMonitoringTimeElapsed(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<LogMonitoringTimeElapsedMiddleware>();
    }

    // ❌ Not AOT-compatible
    /// <summary>Adds the <see cref="LogMonitoringTimeElapsedMiddleware"/> to the request pipeline with an additional callback. This overload is not compatible with native AOT.</summary>
    /// <param name="app">The application builder.</param>
    /// <param name="extraLog">The action called with the declaring type of the endpoint and the elapsed time.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    [RequiresDynamicCode("Using a delegate in UseMiddleware requires reflection which is not supported in AOT.")]
    public static IApplicationBuilder AddMonitoringTimeElapsed(this IApplicationBuilder app, Action<Type, TimeSpan> extraLog)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<LogMonitoringTimeElapsedMiddleware>(extraLog);
    }
}

