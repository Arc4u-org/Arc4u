using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Arc4u.AspNetCore.Results;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>
/// Global filter to manage exceptions.
/// Logs the exception (with the ActivityId to use to retrieve it in the log and the request context: HTTP method, path, action and route values)
/// and sets an <c>ObjectResult</c> carrying problem details.
/// The result is a 403 for an <see cref="UnauthorizedAccessException"/> and a generic 500 (mentioning the ActivityId) for any other exception.
/// When <see cref="ManageExceptionsFilterOptions.IncludeExceptionDetails"/> allows it (by default in the <c>Development</c> environment only),
/// the problem details also carry the details of the exception in the <c>exception</c> extension.
/// When <see cref="ManageExceptionsFilterOptions.LogActionArguments"/> is enabled, the arguments of the action are also logged with the exception:
/// the filter must then be registered as a global (or controller) filter so it also runs as an action filter.
/// </summary>
public class ManageExceptionsFilter : IAsyncExceptionFilter, IAsyncActionFilter
{
    // The key of HttpContext.Items holding the arguments of the executing action; Items is used because a filter instance can be shared between requests.
    private static readonly object ActionArgumentsKey = new();

    // Guards against a cyclic or very deep chain of inner exceptions.
    private const int MaxInnerExceptionDepth = 10;

    private readonly ILogger<ManageExceptionsFilter> _logger;
    private readonly bool _includeExceptionDetails;
    private readonly bool _logActionArguments;
    private readonly int _maxActionArgumentLength;

    /// <summary>
    /// Initializes a filter that never returns the details of the exception and does not log the action arguments.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ManageExceptionsFilter(ILogger<ManageExceptionsFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes a filter that returns the details of the exception and logs the action arguments as configured by <paramref name="options"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options of the filter.</param>
    /// <param name="environment">The host environment, used when <see cref="ManageExceptionsFilterOptions.IncludeExceptionDetails"/> is not set.</param>
    [ActivatorUtilitiesConstructor]
    public ManageExceptionsFilter(ILogger<ManageExceptionsFilter> logger, IOptions<ManageExceptionsFilterOptions> options, IHostEnvironment environment)
        : this(logger)
    {
        _includeExceptionDetails = options.Value.IncludeExceptionDetails ?? environment.IsDevelopment();
        _logActionArguments = options.Value.LogActionArguments;
        _maxActionArgumentLength = options.Value.MaxActionArgumentLength;
    }

    /// <inheritdoc/>
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_logActionArguments && context.ActionArguments.Count > 0)
        {
            // Only keep a copy: the arguments are serialized when an exception occurs, so a successful call costs nothing more.
            context.HttpContext.Items[ActionArgumentsKey] = new Dictionary<string, object?>(context.ActionArguments);
        }

        return next();
    }

    /// <inheritdoc/>
    public Task OnExceptionAsync(ExceptionContext context)
    {
        // The logger writes the trace id of the current activity as ActivityId; start one if there is none so the id returned is in the log.
        using var activity = Activity.Current is null ? new Activity(nameof(ManageExceptionsFilter)).Start() : null;
        var activityId = Activity.Current!.TraceId.ToString();

        // First log the exception, with the context of the request so the failing call can be identified.
        var request = context.HttpContext.Request;
        _logger.Technical()
               .Add("HttpMethod", request.Method)
               .Add("Path", request.Path.Value ?? string.Empty)
               .AddIfNotExist("Action", context.ActionDescriptor.DisplayName)
               .AddIf(context.RouteData.Values.Count > 0, "RouteValues", () => context.RouteData.Values.ToDictionary(v => v.Key, v => v.Value?.ToString()))
               .AddIf(_logActionArguments && context.HttpContext.Items.ContainsKey(ActionArgumentsKey), "ActionArguments",
                      () => ToLoggedArguments((IDictionary<string, object?>)context.HttpContext.Items[ActionArgumentsKey]!))
               .LogException(context.Exception);

        var problem = CreateProblemDetails(context.Exception, activityId, _includeExceptionDetails);

        context.Result = new ObjectResult(problem);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds the problem details returned for <paramref name="exception"/>; shared with <see cref="ManageExceptionsHandler"/> so MVC and the other endpoints answer the same way.
    /// </summary>
    internal static ProblemDetails CreateProblemDetails(Exception exception, string activityId, bool includeExceptionDetails)
    {
        ProblemDetails problem;
        switch (exception)
        {
            case UnauthorizedAccessException:
                problem = new ProblemDetails()
                                .WithTitle("Unauthorized")
                                .WithDetail("You are not allowed to perform this operation")
                                .WithStatusCode(StatusCodes.Status403Forbidden)
                                .WithSeverity("Error")
                                .WithType(new Uri("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unauthorized"));
                break;
            default:
                problem = new ProblemDetails()
                                .WithTitle("Unexpected error.")
                                .WithDetail($"A technical error occurred, contact the application owner. A message has been logged with id: {activityId}")
                                .WithStatusCode(StatusCodes.Status500InternalServerError)
                                .WithSeverity("Error")
                                .WithType(new Uri("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error"));
                break;
        }

        if (includeExceptionDetails)
        {
            problem.WithMetadata("exception", ToDetail(exception, 0));
        }

        return problem;
    }

    private Dictionary<string, string?> ToLoggedArguments(IDictionary<string, object?> arguments)
    {
        return arguments.ToDictionary(a => a.Key, a => ToLoggedArgument(a.Value));
    }

    // MVC actions are not trimming or AOT compatible; when reflection-based serialization is disabled, Serialize throws and the argument is logged as not serializable.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Action arguments are only known at runtime; a failure is caught and logged as not serializable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Action arguments are only known at runtime; a failure is caught and logged as not serializable.")]
    private string? ToLoggedArgument(object? argument)
    {
        switch (argument)
        {
            case null:
                return null;
            case CancellationToken or Stream or IFormFile or IFormFileCollection or HttpContext:
                return $"<{argument.GetType().Name}>";
        }

        string json;
        try
        {
            json = JsonSerializer.Serialize(argument, argument.GetType());
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Logging the exception must never fail because an argument is not serializable (or one of its getters throws).
            return $"<{argument.GetType().Name}: not serializable>";
        }

        return json.Length > _maxActionArgumentLength ? string.Concat(json.AsSpan(0, Math.Max(0, _maxActionArgumentLength)), "...") : json;
    }

    private static ExceptionDetail ToDetail(Exception exception, int depth)
    {
        return new ExceptionDetail(exception.GetType().FullName ?? exception.GetType().Name,
                                   exception.Message,
                                   exception.StackTrace,
                                   exception.InnerException is not null && depth < MaxInnerExceptionDepth ? ToDetail(exception.InnerException, depth + 1) : null);
    }

    /// <summary>
    /// The details of an exception returned to the caller when <see cref="ManageExceptionsFilterOptions.IncludeExceptionDetails"/> allows it.
    /// </summary>
    /// <param name="Type">The full name of the exception type.</param>
    /// <param name="Message">The message of the exception.</param>
    /// <param name="StackTrace">The stack trace of the exception, when any.</param>
    /// <param name="InnerException">The details of the inner exception, when any.</param>
    public sealed record ExceptionDetail(string Type, string Message, string? StackTrace, ExceptionDetail? InnerException);
}
