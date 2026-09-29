using Microsoft.AspNetCore.Builder;

namespace Arc4u.AspNetCore.Middleware;

/// <summary>
/// Registration of <see cref="LogGrpcMonitoringTimeElapsedMiddleware"/> in the request pipeline.
/// </summary>
public static class LogGrpcMonitoringTimeElapsedMiddlewareExtension
{
    /// <summary>
    /// Adds the <see cref="LogGrpcMonitoringTimeElapsedMiddleware"/> to the pipeline, which logs the time each gRPC call takes.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="extraLog">An optional callback invoked with the service type and the elapsed time of each gRPC call, for example to feed a metric.</param>
    /// <returns>The <paramref name="app"/>, to chain calls.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.AddGrpcMonitoringTimeElapsed((serviceType, elapsed) =&gt; Console.WriteLine($"{serviceType.Name}: {elapsed}"));
    /// </code>
    /// </example>
    public static IApplicationBuilder AddGrpcMonitoringTimeElapsed(this IApplicationBuilder app, Action<Type, TimeSpan>? extraLog = null)
    {
        if (null != extraLog)
        {
            return app.UseMiddleware<LogGrpcMonitoringTimeElapsedMiddleware>(extraLog);
        }

        return app.UseMiddleware<LogGrpcMonitoringTimeElapsedMiddleware>();
    }
}
