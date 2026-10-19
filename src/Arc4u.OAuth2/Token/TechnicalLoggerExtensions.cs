using Arc4u.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>Logs the exceptions of the token providers and of the sign-out, whatever the logger registered by the application.</summary>
public static class TechnicalLoggerExtensions
{
    /// <summary>
    /// Logs <paramref name="exception"/> in the <see cref="MessageCategory.Technical"/> category of the Arc4u logger, or as an error of the plain <see cref="ILogger"/>
    /// when the logger is not an Arc4u logger (<c>services.AddILogger()</c> not called), so logging never throws in a catch block.
    /// </summary>
    /// <typeparam name="T">The category of the logger.</typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message written with the plain <see cref="ILogger"/>.</param>
    public static void LogTechnicalException<T>(this ILogger<T> logger, Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            logger.Technical().LogException(exception);
        }
        catch (InvalidOperationException)
        {
            logger.LogError(exception, message);
        }
    }
}
