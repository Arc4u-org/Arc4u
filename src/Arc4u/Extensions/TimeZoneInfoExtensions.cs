using System.Globalization;
using Arc4u;

namespace System;

/// <summary>
/// Provides a set of static methods extending the <see cref="TimeZoneInfo"/> class.
/// </summary>
/// <remarks>
/// The methods read the UTC offset of the time zone instead of its adjustment rules. The rules are not the same on every
/// operating system: on Linux and macOS they are built from the IANA database and change every year, while Windows uses
/// a few floating rules. The results are therefore the same on every operating system.
/// </remarks>
public static class TimeZoneInfoExtensions
{
    // Each day is probed: a time zone never changes its offset twice in the same day.
    private static readonly long StepInSeconds = (long)TimeSpan.FromDays(1).TotalSeconds;

    /// <summary>
    /// Gets the changes of the UTC offset of <paramref name="timeZone"/> between <paramref name="from"/> and <paramref name="to"/>.
    /// </summary>
    /// <param name="timeZone">The time zone.</param>
    /// <param name="from">The start of the period, inclusive.</param>
    /// <param name="to">The end of the period, exclusive.</param>
    /// <returns>The changes, in chronological order, at the precision of the second. The list is empty when <paramref name="to"/> is not after <paramref name="from"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// var brussels = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
    /// var changes = brussels.GetTransitions(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    ///                                       new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
    /// // 2026-03-29 01:00 UTC (+01:00 to +02:00) and 2026-10-25 01:00 UTC (+02:00 to +01:00)
    /// </code>
    /// </example>
    public static IReadOnlyList<TimeZoneTransition> GetTransitions(this TimeZoneInfo timeZone, DateTimeOffset from, DateTimeOffset to)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var transitions = new List<TimeZoneTransition>();

        var current = from.ToUnixTimeSeconds();
        var end = to.ToUnixTimeSeconds();
        var state = GetState(timeZone, current);

        while (current < end)
        {
            var next = Math.Min(current + StepInSeconds, end);
            var nextState = GetState(timeZone, next);

            if (nextState == state)
            {
                current = next;
                continue;
            }

            // The offset changes in (current, next]: find the first second with the new offset.
            var low = current;
            var high = next;
            while (high - low > 1)
            {
                var middle = low + (high - low) / 2;
                if (GetState(timeZone, middle) == state)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            var newState = GetState(timeZone, high);
            transitions.Add(new TimeZoneTransition(DateTimeOffset.FromUnixTimeSeconds(high), state.Offset, newState.Offset, newState.IsDaylightSavingTime));

            current = high;
            state = newState;
        }

        return transitions;
    }

    /// <summary>
    /// Gets the start and the end of the daylight saving time of <paramref name="timeZone"/> in <paramref name="year"/>.
    /// </summary>
    /// <param name="timeZone">The time zone.</param>
    /// <param name="year">The year.</param>
    /// <returns>
    /// The daylight saving period, or <see langword="null"/> when the time zone does not both enter and leave the daylight saving time in <paramref name="year"/>.
    /// <see cref="DaylightTime.Start"/> and <see cref="DaylightTime.End"/> are wall-clock times of the time zone, read on a clock still showing the time before the change
    /// (02:00 and 03:00 in Brussels), with a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Unspecified"/>.
    /// In the southern hemisphere the daylight saving time ends before it starts again in the same year: <see cref="DaylightTime.End"/> is then before <see cref="DaylightTime.Start"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year"/> is not between 1 and 9999.</exception>
    /// <example>
    /// <code language="csharp">
    /// var changes = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels").GetDaylightChanges(2026);
    /// // Start = 2026-03-29 02:00, End = 2026-10-25 03:00, Delta = 01:00
    /// </code>
    /// </example>
    public static DaylightTime? GetDaylightChanges(this TimeZoneInfo timeZone, int year)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);

        if (!timeZone.SupportsDaylightSavingTime)
        {
            return null;
        }

        // A day of margin on both sides catches the changes of the local year that happen in the previous or the next UTC year.
        var from = year > 1 ? new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(-1) : DateTimeOffset.MinValue;
        var to = year < 9999 ? new DateTimeOffset(year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(1) : DateTimeOffset.MaxValue;

        TimeZoneTransition? start = null;
        TimeZoneTransition? end = null;
        var isDaylightSavingTime = timeZone.IsDaylightSavingTime(from);

        foreach (var transition in timeZone.GetTransitions(from, to))
        {
            if (transition.LocalTimeBefore.Year == year)
            {
                if (!isDaylightSavingTime && transition.IsDaylightSavingTimeAfter)
                {
                    start ??= transition;
                }
                else if (isDaylightSavingTime && !transition.IsDaylightSavingTimeAfter)
                {
                    end ??= transition;
                }
            }

            isDaylightSavingTime = transition.IsDaylightSavingTimeAfter;
        }

        if (start is null || end is null)
        {
            return null;
        }

        return new DaylightTime(start.Value.LocalTimeBefore, end.Value.LocalTimeBefore, start.Value.OffsetAfter - start.Value.OffsetBefore);
    }

    /// <summary>
    /// Converts a wall-clock time of <paramref name="timeZone"/> to UTC, whatever its <see cref="DateTime.Kind"/>.
    /// A time that does not exist (skipped when the clocks go forward) is moved forward by the gap: 02:30 becomes 03:30.
    /// A time that happens twice (when the clocks go back) is the first of the two.
    /// </summary>
    internal static DateTime ConvertWallClockToUtc(this TimeZoneInfo timeZone, DateTime wallClock)
    {
        var local = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

        TimeSpan offset;
        if (timeZone.IsInvalidTime(local))
        {
            // The offset before the gap gives the instant the clock would show if it had not jumped.
            offset = timeZone.GetUtcOffset(local.AddDays(-1));
        }
        else if (timeZone.IsAmbiguousTime(local))
        {
            offset = timeZone.GetAmbiguousTimeOffsets(local).Max();
        }
        else
        {
            offset = timeZone.GetUtcOffset(local);
        }

        return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
    }

    private static (TimeSpan Offset, bool IsDaylightSavingTime) GetState(TimeZoneInfo timeZone, long unixTimeSeconds)
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        return (timeZone.GetUtcOffset(instant), timeZone.IsDaylightSavingTime(instant));
    }
}
