namespace Arc4u;

/// <summary>
/// A change of the UTC offset of a time zone, for example the start or the end of the daylight saving time.
/// </summary>
/// <param name="Instant">The UTC instant of the change.</param>
/// <param name="OffsetBefore">The UTC offset in effect just before <paramref name="Instant"/>.</param>
/// <param name="OffsetAfter">The UTC offset in effect from <paramref name="Instant"/>.</param>
/// <param name="IsDaylightSavingTimeAfter">Whether the daylight saving time is in effect from <paramref name="Instant"/>.</param>
public readonly record struct TimeZoneTransition(DateTimeOffset Instant, TimeSpan OffsetBefore, TimeSpan OffsetAfter, bool IsDaylightSavingTimeAfter)
{
    /// <summary>
    /// Gets the wall-clock time of the change, read on a clock still showing the time before the change
    /// (for example 02:00 for the start of the summer time in Brussels and 03:00 for its end).
    /// </summary>
    public DateTime LocalTimeBefore => DateTime.SpecifyKind(Instant.UtcDateTime + OffsetBefore, DateTimeKind.Unspecified);
}
