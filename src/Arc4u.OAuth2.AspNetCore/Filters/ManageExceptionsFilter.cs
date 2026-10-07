using System.Diagnostics;
using Arc4u.AspNetCore.Results;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>
/// Global filter to manage exceptions.
/// Logs the exception (with the ActivityId to use to retrieve it in the log) and sets an <c>ObjectResult</c> carrying problem details.
/// The result is a 403 for an <see cref="UnauthorizedAccessException"/> and a generic 500 (mentioning the ActivityId) for any other exception.
/// </summary>
/// <param name="logger">The logger.</param>
public class ManageExceptionsFilter(ILogger<ManageExceptionsFilter> logger) : IAsyncExceptionFilter
{
    /// <inheritdoc/>
    public Task OnExceptionAsync(ExceptionContext context)
    {
        // The logger writes the trace id of the current activity as ActivityId; start one if there is none so the id returned is in the log.
        using var activity = Activity.Current is null ? new Activity(nameof(ManageExceptionsFilter)).Start() : null;
        var activityId = Activity.Current!.TraceId.ToString();

        // First log the exception.
        logger.Technical().LogException(context.Exception);

        switch (context.Exception)
        {
            case UnauthorizedAccessException:
                context.Result = new ObjectResult(new ProblemDetails()
                                                        .WithTitle("Unauthorized")
                                                        .WithDetail("You are not allowed to perform this operation")
                                                        .WithStatusCode(StatusCodes.Status403Forbidden)
                                                        .WithSeverity("Error")
                                                        .WithType(new Uri("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unauthorized")));
                break;
            default:
                context.Result = new ObjectResult(new ProblemDetails()
                                                        .WithTitle("Unexpected error.")
                                                        .WithDetail($"A technical error occurred, contact the application owner. A message has been logged with id: {activityId}")
                                                        .WithStatusCode(StatusCodes.Status500InternalServerError)
                                                        .WithSeverity("Error")
                                                        .WithType(new Uri("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error")));
                break;
        }

        return Task.CompletedTask;
    }
}
