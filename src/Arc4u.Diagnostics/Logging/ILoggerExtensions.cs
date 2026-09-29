using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Arc4u.Diagnostics;
/// <summary>
/// Extension methods on <see cref="ILogger"/> and <see cref="ILogger{TCategoryName}"/> that select the Arc4u message category
/// (<see cref="MessageCategory"/>) and return an <see cref="ILoggerWrapper{T}"/> on which properties can be added fluently before the message is written.
/// </summary>
/// <remarks>
/// The logger must have been resolved from a container configured with
/// <see cref="Arc4u.Dependency.ServicesRegistrationExtension.AddILogger(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>;
/// otherwise an <see cref="InvalidOperationException"/> is thrown.
/// </remarks>
/// <example>
/// <code language="csharp">
/// _logger.Technical().Add("OrderId", orderId).LogInformation("Order processed.");
/// </code>
/// </example>
public static class ILoggerExtensions
{
    /// <summary>Starts a log entry in the <see cref="MessageCategory.Technical"/> category (messages for IT people).</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger, which must be an <see cref="ILoggerWrapper{T}"/>.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Technical category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/>.</exception>
    public static ILoggerWrapper<T> Technical<T>(this ILogger<T> logger, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<T> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Technical), methodName) : throw new InvalidOperationException("Bad Arc4u usage.");

    /// <summary>Starts a log entry in the <see cref="MessageCategory.Business"/> category (messages for IT and business people).</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger, which must be an <see cref="ILoggerWrapper{T}"/>.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Business category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/>.</exception>
    public static ILoggerWrapper<T> Business<T>(this ILogger<T> logger, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<T> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Business), methodName) : throw new InvalidOperationException("Bad Arc4u usage.");

    /// <summary>Starts a log entry in the <see cref="MessageCategory.Monitoring"/> category (monitoring purposes).</summary>
    /// <typeparam name="T">The category type of the logger.</typeparam>
    /// <param name="logger">The logger, which must be an <see cref="ILoggerWrapper{T}"/>.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Monitoring category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/>.</exception>
    public static ILoggerWrapper<T> Monitoring<T>(this ILogger<T> logger, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<T> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Monitoring), methodName) : throw new InvalidOperationException("Bad Arc4u usage.");

    /// <summary>Starts a log entry in the <see cref="MessageCategory.Technical"/> category for a non-generic <see cref="ILogger"/>, using <paramref name="specificType"/> as the source context.</summary>
    /// <param name="logger">The non-generic logger, which must be an <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</param>
    /// <param name="specificType">The type written in the <see cref="LoggingConstants.Class"/> property.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Technical category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</exception>
    public static ILoggerWrapper<DefaultLogger> Technical(this ILogger logger, Type specificType, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<DefaultLogger> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Technical), methodName, specificType) : throw new InvalidOperationException("Bad Arc4u usage.");

    /// <summary>Starts a log entry in the <see cref="MessageCategory.Technical"/> category for a non-generic <see cref="ILogger"/>, using <typeparamref name="T"/> as the source context.</summary>
    /// <typeparam name="T">The type written in the <see cref="LoggingConstants.Class"/> property.</typeparam>
    /// <param name="logger">The non-generic logger, which must be an <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Technical category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</exception>
    public static ILoggerWrapper<DefaultLogger> Technical<T>(this ILogger logger, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<DefaultLogger> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Technical), methodName, typeof(T)) : throw new InvalidOperationException("Bad Arc4u usage.");

    /// <summary>Starts a log entry in the <see cref="MessageCategory.Monitoring"/> category for a non-generic <see cref="ILogger"/>, using <paramref name="specificType"/> as the source context.</summary>
    /// <param name="logger">The non-generic logger, which must be an <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</param>
    /// <param name="specificType">The type written in the <see cref="LoggingConstants.Class"/> property.</param>
    /// <param name="methodName">The name of the calling member; filled in by the compiler.</param>
    /// <returns>The logger wrapper positioned on the Monitoring category.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="logger"/> is not an Arc4u <see cref="ILoggerWrapper{T}"/> of <see cref="DefaultLogger"/>.</exception>
    public static ILoggerWrapper<DefaultLogger> Monitoring(this ILogger logger, Type specificType, [CallerMemberName] string methodName = "") =>
        logger is ILoggerWrapper<DefaultLogger> loggerWraper ? loggerWraper.SetContext(nameof(MessageCategory.Monitoring), methodName, specificType) : throw new InvalidOperationException("Bad Arc4u usage.");

}
