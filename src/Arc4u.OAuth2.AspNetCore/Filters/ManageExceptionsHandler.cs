using System.Diagnostics;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>
/// Exception handler for the exceptions the <see cref="ManageExceptionsFilter"/> does not see: minimal API endpoints, middlewares, and the writing of an MVC result.
/// Logs the exception (with the ActivityId and the request context: HTTP method, path, endpoint and route values) and writes the same problem details as
/// <see cref="ManageExceptionsFilter"/>: a 403 for an <see cref="UnauthorizedAccessException"/> and a generic 500 (mentioning the ActivityId) for any other exception.
/// </summary>
/// <remarks>
/// Register it with <see cref="ManageExceptionsHandlerExtension.AddManageExceptionsHandler"/> and add <c>app.UseExceptionHandler()</c> to the pipeline.
/// It uses the <see cref="ManageExceptionsFilterOptions.IncludeExceptionDetails"/> option; <see cref="ManageExceptionsFilterOptions.LogActionArguments"/> only applies to MVC actions.
/// </remarks>
public sealed class ManageExceptionsHandler : IExceptionHandler
{
    private readonly ILogger<ManageExceptionsHandler> _logger;
    private readonly bool _includeExceptionDetails;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageExceptionsHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options shared with <see cref="ManageExceptionsFilter"/>.</param>
    /// <param name="environment">The host environment, used when <see cref="ManageExceptionsFilterOptions.IncludeExceptionDetails"/> is not set.</param>
    public ManageExceptionsHandler(ILogger<ManageExceptionsHandler> logger, IOptions<ManageExceptionsFilterOptions> options, IHostEnvironment environment)
    {
        _logger = logger;
        _includeExceptionDetails = options.Value.IncludeExceptionDetails ?? environment.IsDevelopment();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Nothing can be written once the response has started: let the middleware abort the request.
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        // The logger writes the trace id of the current activity as ActivityId; start one if there is none so the id returned is in the log.
        using var activity = Activity.Current is null ? new Activity(nameof(ManageExceptionsHandler)).Start() : null;
        var activityId = Activity.Current!.TraceId.ToString();

        // The exception handler middleware clears the endpoint and the route values (and may rewrite the path) before calling the handlers:
        // the original ones are kept by the feature.
        var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
        var routeValues = feature?.RouteValues ?? httpContext.Request.RouteValues;
        _logger.Technical()
               .Add("HttpMethod", httpContext.Request.Method)
               .Add("Path", feature?.Path ?? httpContext.Request.Path.Value ?? string.Empty)
               .AddIfNotExist("Endpoint", (feature?.Endpoint ?? httpContext.GetEndpoint())?.DisplayName)
               .AddIf(routeValues.Count > 0, "RouteValues", () => routeValues.ToDictionary(v => v.Key, v => v.Value?.ToString()))
               .LogException(exception);

        var problem = ManageExceptionsFilter.CreateProblemDetails(exception, activityId, _includeExceptionDetails);

        await TypedResults.Problem(problem).ExecuteAsync(httpContext).ConfigureAwait(false);

        return true;
    }
}
