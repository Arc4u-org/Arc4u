using Arc4u.Diagnostics;

namespace Arc4u.Diagnostics;

/// <summary>Fluent extension methods that add properties to the next log entry of an <see cref="ILoggerWrapper{T}"/>.</summary>
/// <remarks>The keys defined in <see cref="LoggingConstants"/> are reserved: using one of them throws a <see cref="ReservedLoggingKeyException"/>.</remarks>
public static class LoggerWrapperExtensions
{
    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> Add<T>(this ILoggerWrapper<T> logger, string key, object value)
    {
        logger.AdditionalFields[ValidateKey(key)] = value;
        return logger;
    }

    /// <summary>
    /// Adds the specified key when condition is true
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="condition">if set to <c>true</c> [condition].</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddIf<T>(this ILoggerWrapper<T> logger, bool condition, string key, Func<object> value)
    {
        if (condition)
        {
            logger.AdditionalFields[ValidateKey(key)] = value();
        }
        return logger;
    }

    /// <summary>
    /// Adds the specified key if doesn't not exist.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddIfNotExist<T>(this ILoggerWrapper<T> logger, string key, object? value)
    {
        var validKey = ValidateKey(key);

        if (value == null || logger.AdditionalFields.ContainsKey(validKey))
        {
            return logger;
        }
        logger.AdditionalFields[validKey] = value;
        return logger;
    }

    /// <summary>
    /// Adds the specified key or replaces it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddOrReplace<T>(this ILoggerWrapper<T> logger, string key, object value)
    {
        logger.AdditionalFields[ValidateKey(key)] = value;
        return logger;
    }

    /// <summary>
    /// Adds the specified key or replaces it when the condition is true.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="condition">if set to <c>true</c> [condition].</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddOrReplaceIf<T>(this ILoggerWrapper<T> logger, bool condition, string key, Func<double> value)
    {

        if (condition)
        {
            logger.AdditionalFields[ValidateKey(key)] = value();
        }
        return logger;
    }

    /// <summary>
    /// Adds the stack trace.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddStackTrace<T>(this ILoggerWrapper<T> logger)
    {
        logger.IncludeStackTrace = true;
        return logger;
    }

    /// <summary>
    /// Add the Total Memory usage
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="logger">The logger.</param>
    /// <returns></returns>
    public static ILoggerWrapper<T> AddMemoryUsage<T>(this ILoggerWrapper<T> logger)
    {
        return logger.Add("Memory", GC.GetTotalMemory(false));
    }
    /// <summary>
    /// Validates the key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">key</exception>
    /// <exception cref="ReservedLoggingKeyException"></exception>
    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        return key switch
        {
            LoggingConstants.ActivityId or
            LoggingConstants.Application or
            LoggingConstants.Category or
            LoggingConstants.Class or
            LoggingConstants.Identity or
            LoggingConstants.MethodName or
            LoggingConstants.ProcessId or
            LoggingConstants.Stacktrace or
            LoggingConstants.ThreadId =>
            throw new ReservedLoggingKeyException(key),
            _ => key,
        };
    }
}
