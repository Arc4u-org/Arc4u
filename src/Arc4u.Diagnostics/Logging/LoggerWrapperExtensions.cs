using Arc4u.Diagnostics;

namespace Arc4u.Diagnostics;

/// <summary>Fluent extension methods that add properties to the next log entry of an <see cref="ILoggerWrapper{T}"/>.</summary>
/// <remarks>The keys defined in <see cref="LoggingConstants"/> are reserved: using one of them throws a <see cref="ReservedLoggingKeyException"/>.</remarks>
public static class LoggerWrapperExtensions
{
    /// <summary>Adds a property to the next log entry, replacing the value if the key is already present.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>, empty or white space.</exception>
    /// <exception cref="ReservedLoggingKeyException"><paramref name="key"/> is one of the names reserved in <see cref="LoggingConstants"/>.</exception>
    public static ILoggerWrapper<T> Add<T>(this ILoggerWrapper<T> logger, string key, object value)
    {
        logger.AdditionalFields[ValidateKey(key)] = value;
        return logger;
    }

    /// <summary>Adds a property to the next log entry, replacing the value if the key is already present, only when the condition is <see langword="true"/>.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <param name="condition">When <see langword="false"/>, nothing is added and <paramref name="value"/> is not evaluated.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">A function that provides the property value; it is called only when the condition is <see langword="true"/>.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>, empty or white space and the condition is <see langword="true"/>.</exception>
    /// <exception cref="ReservedLoggingKeyException"><paramref name="key"/> is reserved in <see cref="LoggingConstants"/> and the condition is <see langword="true"/>.</exception>
    public static ILoggerWrapper<T> AddIf<T>(this ILoggerWrapper<T> logger, bool condition, string key, Func<object> value)
    {
        if (condition)
        {
            logger.AdditionalFields[ValidateKey(key)] = value();
        }
        return logger;
    }

    /// <summary>Adds a property to the next log entry only when the key is not already present and the value is not <see langword="null"/>; otherwise nothing changes.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value; a <see langword="null"/> value is ignored.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>, empty or white space.</exception>
    /// <exception cref="ReservedLoggingKeyException"><paramref name="key"/> is one of the names reserved in <see cref="LoggingConstants"/>.</exception>
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

    /// <summary>Adds a property to the next log entry, or replaces its value if the key is already present.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>, empty or white space.</exception>
    /// <exception cref="ReservedLoggingKeyException"><paramref name="key"/> is one of the names reserved in <see cref="LoggingConstants"/>.</exception>
    public static ILoggerWrapper<T> AddOrReplace<T>(this ILoggerWrapper<T> logger, string key, object value)
    {
        logger.AdditionalFields[ValidateKey(key)] = value;
        return logger;
    }

    /// <summary>Adds a numeric property to the next log entry, or replaces its value if the key is already present, only when the condition is <see langword="true"/>.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <param name="condition">When <see langword="false"/>, nothing is added and <paramref name="value"/> is not evaluated.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">A function that provides the numeric property value; it is called only when the condition is <see langword="true"/>.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>, empty or white space and the condition is <see langword="true"/>.</exception>
    /// <exception cref="ReservedLoggingKeyException"><paramref name="key"/> is reserved in <see cref="LoggingConstants"/> and the condition is <see langword="true"/>.</exception>
    public static ILoggerWrapper<T> AddOrReplaceIf<T>(this ILoggerWrapper<T> logger, bool condition, string key, Func<double> value)
    {

        if (condition)
        {
            logger.AdditionalFields[ValidateKey(key)] = value();
        }
        return logger;
    }

    /// <summary>Requests that the stack trace is added, in the <see cref="LoggingConstants.Stacktrace"/> property, to the log entry: the stack trace of the exception when one is logged, otherwise the current stack trace.</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
    public static ILoggerWrapper<T> AddStackTrace<T>(this ILoggerWrapper<T> logger)
    {
        logger.IncludeStackTrace = true;
        return logger;
    }

    /// <summary>Adds a <c>Memory</c> property holding the number of bytes currently thought to be allocated (<see cref="GC.GetTotalMemory(bool)"/> without forcing a collection).</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger wrapper.</param>
    /// <returns>The same logger wrapper, for chaining.</returns>
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
