using System.Globalization;

namespace System;

/// <summary>
/// Provides a set of static methods extending the <see cref="TimeProvider"/> class.
/// They work in the <see cref="TimeProvider.LocalTimeZone"/> of the provider, which is the time zone of the application with a <c>ZonedTimeProvider</c>.
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>
    /// Gets the start and the end of the daylight saving time of the <see cref="TimeProvider.LocalTimeZone"/> in <paramref name="year"/>.
    /// See <see cref="TimeZoneInfoExtensions.GetDaylightChanges(TimeZoneInfo, int)"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="year">The year.</param>
    /// <returns>The daylight saving period, or <see langword="null"/> when the time zone does not both enter and leave the daylight saving time in <paramref name="year"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year"/> is not between 1 and 9999.</exception>
    public static DaylightTime? GetDaylightChanges(this TimeProvider timeProvider, int year)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return timeProvider.LocalTimeZone.GetDaylightChanges(year);
    }

    /// <summary>
    /// Gets the time to wait from now until the next occurrence of <paramref name="timeOfDay"/> in the <see cref="TimeProvider.LocalTimeZone"/>,
    /// taking the changes of the daylight saving time into account. Typically used as the due time of a daily timer.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="timeOfDay">The wall-clock time of the day. A time skipped when the clocks go forward is moved forward by the gap (02:30 becomes 03:30);
    /// a time that happens twice when the clocks go back is the first of the two.</param>
    /// <returns>The time to wait, <see cref="TimeSpan.Zero"/> when <paramref name="timeOfDay"/> is now or negative,
    /// or negative one (-1) millisecond (no start) when <paramref name="timeOfDay"/> is greater than one day.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// // Run every day at 02:00 in the time zone of the application.
    /// var timer = timeProvider.CreateTimer(_ => Purge(), null, timeProvider.GetDueTimeOfDay(TimeSpan.FromHours(2)), Timeout.InfiniteTimeSpan);
    /// </code>
    /// </example>
    public static TimeSpan GetDueTimeOfDay(this TimeProvider timeProvider, TimeSpan timeOfDay)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        //consider no start
        if (timeOfDay > TimeSpan.FromDays(1))
        {
            return TimeSpan.FromMilliseconds(-1);
        }

        //consider immediate start
        if (timeOfDay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var timeZone = timeProvider.LocalTimeZone;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var today = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date;

        var dueDate = timeZone.ConvertWallClockToUtc(today + timeOfDay);
        if (dueDate < utcNow)
        {
            dueDate = timeZone.ConvertWallClockToUtc(today.AddDays(1) + timeOfDay);
        }

        return dueDate - utcNow;
    }
}
