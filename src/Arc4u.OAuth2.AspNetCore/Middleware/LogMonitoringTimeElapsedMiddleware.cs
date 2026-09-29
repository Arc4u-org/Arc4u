using System.Diagnostics;
using System.Reflection;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Middleware;

/// <summary>
/// A middleware that measures the time spent by each request that is handled by a controller action or a minimal API endpoint and logs it as a monitoring message
/// (elapsed milliseconds, status code and endpoint name).
/// </summary>
public class LogMonitoringTimeElapsedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Action<Type, TimeSpan>? _log;

    /// <summary>Initializes a new instance of the <see cref="LogMonitoringTimeElapsedMiddleware"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public LogMonitoringTimeElapsedMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _log = null;
    }

    /// <summary>Initializes a new instance of the <see cref="LogMonitoringTimeElapsedMiddleware"/> class with an additional callback.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="extraLog">The action called with the declaring type of the endpoint and the elapsed time, for example to publish a metric.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public LogMonitoringTimeElapsedMiddleware(RequestDelegate next, Action<Type, TimeSpan> extraLog)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _log = extraLog;
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the rest of the pipeline is done.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var logger = context.RequestServices.GetRequiredService<ILogger>();

        var startTimestamp = Stopwatch.GetTimestamp();
        await _next(context).ConfigureAwait(false);
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        try
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                return;
            }

            MemberInfo? methodInfo = null;

            var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (descriptor?.MethodInfo?.DeclaringType is not null)
            {
                // Try to extract MethodInfo for MVC Controller endpoints
                methodInfo = descriptor.MethodInfo;
            }
            else
            {
                // Try to extract MethodInfo for Minimal API endpoints
                methodInfo = endpoint.Metadata.GetMetadata<MethodInfo>();
            }

            if (methodInfo?.DeclaringType is not null)
            {
                var properties = logger.Monitoring(methodInfo.DeclaringType, methodInfo.Name)
                    .Add("Elapsed", elapsed.TotalMilliseconds)
                    .Add("StatusCode", context.Response.StatusCode);

                if (!string.IsNullOrWhiteSpace(endpoint.DisplayName))
                {
                    properties.Add("Endpoint", endpoint.DisplayName);
                }

                properties.LogInformation("Time to complete method call");

                _log?.Invoke(methodInfo.DeclaringType, elapsed);
            }
        }
        catch (Exception ex)
        {
            logger.Technical<LogMonitoringTimeElapsedMiddleware>()
                  .LogException(ex);
        }
    }
}

