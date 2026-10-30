using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u;

/// <summary>
/// Extension methods to register a <see cref="ZonedTimeProvider"/>.
/// </summary>
public static class ZonedTimeProviderExtension
{
    /// <summary>
    /// Registers a <see cref="ZonedTimeProvider"/> as the <see cref="TimeProvider"/> of the application, replacing any <see cref="TimeProvider"/> already registered.
    /// Its time zone is <see cref="Arc4u.Configuration.Environment.TimeZone"/> of the <see cref="ApplicationConfig"/>; when the time zone is empty or cannot be found,
    /// the time zone of the machine is used and a warning is logged.
    /// The <see cref="ApplicationConfig"/> options must be registered (see <c>AddApplicationConfig</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddApplicationConfig(builder.Configuration);
    /// builder.Services.AddZonedTimeProvider();
    ///
    /// public sealed class ShippingService(TimeProvider timeProvider)
    /// {
    ///     public bool ShipsToday() => timeProvider.GetLocalNow().TimeOfDay &lt; new TimeSpan(17, 0, 0);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddZonedTimeProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<ApplicationConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<ZonedTimeProvider>>();

            return new ZonedTimeProvider(FindTimeZone(config.Environment.TimeZone, logger));
        });

        return services;
    }

    /// <summary>
    /// Registers a <see cref="ZonedTimeProvider"/> for a specific time zone as a keyed <see cref="TimeProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key used to inject the provider, for example with <c>[FromKeyedServices("NewYork")]</c>.</param>
    /// <param name="timeZoneId">The identifier of the time zone, as understood by <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="serviceKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="TimeZoneNotFoundException">The time zone <paramref name="timeZoneId"/> is not found.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddZonedTimeProvider("NewYork", "America/New_York");
    ///
    /// public sealed class ReportService([FromKeyedServices("NewYork")] TimeProvider newYork)
    /// {
    ///     public string Format(DateTime createdUtc) => createdUtc.AsLocalTime(newYork).ToString("g");
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddZonedTimeProvider(this IServiceCollection services, string serviceKey, string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentException.ThrowIfNullOrEmpty(timeZoneId);

        services.AddKeyedSingleton<TimeProvider>(serviceKey, new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)));

        return services;
    }

    /// <summary>
    /// Finds the time zone <paramref name="timeZoneId"/>, or returns the time zone of the machine when it is empty or not found.
    /// </summary>
    internal static TimeZoneInfo FindTimeZone<T>(string? timeZoneId, ILogger<T> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            logger.Technical().LogTryToUseTimeZone(timeZoneId ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
        }
        catch (Exception ex)
        {
            logger.Technical().LogZoneNotFound(timeZoneId ?? string.Empty);
            logger.Technical().LogException(ex);
        }

        return TimeZoneInfo.Local;
    }
}
