using System.Diagnostics;
using Arc4u.Diagnostics.Monitoring;
using Arc4u.Diagnostics;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arc4u.AspNetCore.Middleware;

/// <summary>
/// Middleware measuring the duration of gRPC calls and writing it to the technical log.
/// </summary>
/// <remarks>Only requests routed to a gRPC method are logged, with the service type, the method name, the elapsed milliseconds and the response status code. Add it with <see cref="LogGrpcMonitoringTimeElapsedMiddlewareExtension.AddGrpcMonitoringTimeElapsed(Microsoft.AspNetCore.Builder.IApplicationBuilder, Action{Type, TimeSpan})"/>.</remarks>
public class LogGrpcMonitoringTimeElapsedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Action<Type, TimeSpan>? _log;

    /// <summary>
    /// Creates the middleware, which only logs.
    /// </summary>
    /// <param name="next">The next delegate of the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public LogGrpcMonitoringTimeElapsedMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        _log = null;
    }

    /// <summary>
    /// Creates the middleware, which logs and also invokes a callback for each measured call.
    /// </summary>
    /// <param name="next">The next delegate of the pipeline.</param>
    /// <param name="extraLog">The callback invoked with the service type and the elapsed time of each gRPC call.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public LogGrpcMonitoringTimeElapsedMiddleware(RequestDelegate next, Action<Type, TimeSpan> extraLog)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        _log = extraLog;
    }

    /// <summary>
    /// Runs the rest of the pipeline and, for a gRPC endpoint, logs how long it took.
    /// </summary>
    /// <remarks>An exception raised while logging is itself logged and never breaks the request.</remarks>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="logger">The logger, injected per request.</param>
    /// <returns>A task representing the processing of the request.</returns>
    public async Task Invoke(HttpContext context, ILogger logger)
    {
        var startingTimestamp = Stopwatch.GetTimestamp();

        await _next(context).ConfigureAwait(false);

        var elapsed = Stopwatch.GetElapsedTime(startingTimestamp);

        try
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var descriptor = endpoint.Metadata.GetMetadata<GrpcMethodMetadata>();
                if (descriptor != null)
                {
                    logger.Technical(descriptor.ServiceType, descriptor.Method.Name)
                           .Add("Elapsed", elapsed.TotalMilliseconds)
                           .Add("StatusCode", context.Response.StatusCode)
                           .LogTimeToCompleteCall();

                    _log?.Invoke(descriptor.ServiceType, elapsed);
                }
            }

        }
        catch (Exception ex)
        {
            logger.Technical<LogGrpcMonitoringTimeElapsedMiddleware>().LogException(ex);
        }
    }
}
