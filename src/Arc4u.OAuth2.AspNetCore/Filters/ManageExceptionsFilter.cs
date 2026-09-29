using System.Diagnostics;
using Arc4u.AspNetCore.Results;
using Arc4u.Diagnostics;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>
/// Global filter to manage exceptions.
/// Will log the exception and return a BadRequestObjectResult with the ActivityId to use to retrieve the information in the log.
/// Only business messages are returned to the client assoicated with the <c>AppException</c>.
/// </summary>
public class ManageExceptionsFilter(ILogger<ManageExceptionsFilter> logger, IApplicationContext application) : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        // If the activity id is not set, create one. This is the case for anonymous users.
        var activityId = string.IsNullOrEmpty(application?.ActivityID) ? Activity.Current?.Id ?? Guid.NewGuid().ToString() : application?.ActivityID;

        // First log the exception.
        logger.Technical()
              .AddIf(string.IsNullOrEmpty(application?.ActivityID), LoggingConstants.ActivityId, () => activityId!)
              .LogException(context.Exception);

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
                                                        .WithDetail($"A technical error occured, contact the application owner. A message has been logged with id: {activityId}")
                                                        .WithStatusCode(StatusCodes.Status500InternalServerError)
                                                        .WithSeverity("Error")
                                                        .WithType(new Uri("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error")));
                break;
        }

        return Task.CompletedTask;
    }
}
