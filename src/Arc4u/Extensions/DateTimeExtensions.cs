using Arc4u;

namespace System;

/// <summary>
/// Provides a set of static methods extending the <see cref="DateTime"/> class.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a UTC <paramref name="value"/> to the <see cref="TimeProvider.LocalTimeZone"/> of <paramref name="timeProvider"/>.
    /// </summary>
    /// <param name="value">The value. A value whose <see cref="DateTime.Kind"/> is not <see cref="DateTimeKind.Utc"/> is considered to be already in the time zone of <paramref name="timeProvider"/> and is returned unchanged.</param>
    /// <param name="timeProvider">The time provider, typically a <see cref="ZonedTimeProvider"/>.</param>
    /// <returns>The wall-clock time in the time zone of <paramref name="timeProvider"/>. A converted value has a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Unspecified"/>:
    /// <see cref="DateTimeKind.Local"/> would mean the time zone of the machine.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// @inject TimeProvider Clock
    /// &lt;td&gt;@order.CreatedUtc.AsLocalTime(Clock).ToString("g")&lt;/td&gt;
    /// </code>
    /// </example>
    public static DateTime AsLocalTime(this DateTime value, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (value.Kind != DateTimeKind.Utc)
        {
            return value;
        }

        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, timeProvider.LocalTimeZone), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Converts a <paramref name="value"/> to Coordinated Universal Time (UTC).
    /// </summary>
    /// <param name="value">The value:
    /// <list type="bullet">
    /// <item><description><see cref="DateTimeKind.Utc"/>: returned unchanged.</description></item>
    /// <item><description><see cref="DateTimeKind.Local"/>: a time of the machine (for example deserialized from a JSON offset), converted with <see cref="DateTime.ToUniversalTime"/>.</description></item>
    /// <item><description><see cref="DateTimeKind.Unspecified"/>: a wall-clock time in the time zone of <paramref name="timeProvider"/> (for example typed in a form).
    /// A time skipped when the clocks go forward is moved forward by the gap (02:30 becomes 03:30); a time that happens twice when the clocks go back is the first of the two.</description></item>
    /// </list>
    /// </param>
    /// <param name="timeProvider">The time provider, typically a <see cref="ZonedTimeProvider"/>.</param>
    /// <returns>A <see cref="DateTime"/> whose <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static DateTime AsUniversalTime(this DateTime value, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => timeProvider.LocalTimeZone.ConvertWallClockToUtc(value),
        };
    }

    /// <summary>
    /// Gets the elapsed time between this <paramref name="instance"/> and <paramref name="value"/>, taking the changes of the daylight saving time into account.
    /// Both values are converted to UTC as <see cref="AsUniversalTime(DateTime, TimeProvider)"/> does.
    /// </summary>
    /// <param name="instance">The first date and time.</param>
    /// <param name="value">The second date and time.</param>
    /// <param name="timeProvider">The time provider, typically a <see cref="ZonedTimeProvider"/>.</param>
    /// <returns>The absolute duration between the two values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static TimeSpan SubtractWithDaylightTime(this DateTime instance, DateTime value, TimeProvider timeProvider)
    {
        return (instance.AsUniversalTime(timeProvider) - value.AsUniversalTime(timeProvider)).Duration();
    }

    /// <summary>
    /// Converts an instant to the <see cref="TimeProvider.LocalTimeZone"/> of <paramref name="timeProvider"/>, keeping its offset.
    /// </summary>
    /// <param name="value">The instant.</param>
    /// <param name="timeProvider">The time provider, typically a <see cref="ZonedTimeProvider"/>.</param>
    /// <returns>The same instant, with the offset of the time zone of <paramref name="timeProvider"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static DateTimeOffset ToLocalTime(this DateTimeOffset value, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return TimeZoneInfo.ConvertTime(value, timeProvider.LocalTimeZone);
    }
}
