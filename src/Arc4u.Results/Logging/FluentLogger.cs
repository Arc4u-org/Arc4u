using System.Diagnostics;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Arc4u.Results.Logging;

/// <summary>
/// Default <see cref="IResultLogger"/>: writes the outcome of a <see cref="ResultBase"/> to an <see cref="ILogger{TCategoryName}"/>
/// using the Arc4u business logging conventions.
/// </summary>
/// <remarks>
/// Registered as a shared (singleton) service through the <c>Export</c> attribute. For a failed result each error is logged
/// on its own: a <see cref="ValidationError"/> at the level matching its <see cref="Severity"/> (with its code as a property when set),
/// an exceptional error as an exception, any other error (a <see cref="ProblemDetailError"/> for example) at the level requested by the caller
/// (the <c>logLevel</c> of <c>LogIfFailed</c>). The informational reasons of the result are then logged once.
/// Use <see cref="ResultLoggerServiceCollectionExtensions.AddResultLogger"/> to register it and hand it to <c>Result.Setup</c>.
/// </remarks>
[Export(typeof(IResultLogger)), Shared]
public class FluentLogger : IResultLogger
{
    /// <summary>
    /// Creates a logger writing to the given <paramref name="logger"/>.
    /// </summary>
    /// <param name="logger">The logger receiving the business messages.</param>
    public FluentLogger(ILogger<FluentLogger> logger)
    {
        _logger = logger;
    }

    delegate void LogDelegate(string? message, params object?[] args);

    private readonly ILogger<FluentLogger> _logger;
    /// <inheritdoc/>
    public void Log(string context, string content, ResultBase result, LogLevel logLevel)
    {
        if (!string.IsNullOrEmpty(context) && !string.IsNullOrEmpty(content))
        {
            var logger = _logger.Business()
                                .Add("Context", () => context!);

            GetBusinessLogger(logger, logLevel)(content);
        }

        LogErrorsAndReasons(result, logLevel);
    }

    /// <inheritdoc/>
    public void Log<TContext>(string content, ResultBase result, LogLevel logLevel)
    {
        var logger = _logger.Business()
                            .Add("Context", typeof(TContext).FullName!);

        GetBusinessLogger(logger, logLevel)(content);

        LogErrorsAndReasons(result, logLevel);
    }

    private void LogErrorsAndReasons(ResultBase result, LogLevel logLevel)
    {
        if (result is { IsFailed: true, Errors: not null })
        {
            foreach (var error in result.Errors)
            {
                switch (error)
                {
                    case ValidationError validationError:
                    {
                        var logger = _logger.Business()
                            .AddIf(validationError.Code is not null, "Code", () => validationError.Code!);

                        GetBusinessLogger(logger, validationError.Severity)(validationError.Message);
                        break;
                    }
                    case IExceptionalError exceptionalError:
                        _logger.LogException(exceptionalError.Exception);
                        break;
                    default:
                        GetBusinessLogger(_logger.Business(), logLevel)(error.Message);
                        break;
                }
            }
        }

        // Once for the result, not once per error, and never re-logging an error that the loop
        // above already reported at its own severity: what is left are the informational reasons.
        LogReasons(result.Reasons);
    }

    private void LogReasons(IEnumerable<IReason> reasons)
    {
        foreach (var reason in reasons)
        {
            if (reason is IError)
            {
                continue;
            }

            _logger.Business().LogInformation(reason.Message);
        }
    }

    private static LogDelegate GetBusinessLogger(ILoggerWrapper<FluentLogger> logger, Severity severity) => severity switch
    {
        Severity.Error => logger.LogError,
        Severity.Warning => logger.LogWarning,
        Severity.Info => logger.LogInformation,
        _ => logger.LogDebug,
    };

    private static LogDelegate GetBusinessLogger(ILoggerWrapper<FluentLogger> logger, LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => logger.LogTrace,
        LogLevel.Debug => logger.LogDebug,
        LogLevel.Information => logger.LogInformation,
        LogLevel.Warning => logger.LogWarning,
        LogLevel.Error => logger.LogError,
        LogLevel.Critical => logger.LogCritical,
        LogLevel.None => logger.LogTrace,
        _ => logger.LogDebug,
    };
}
