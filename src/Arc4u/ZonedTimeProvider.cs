namespace Arc4u;

/// <summary>
/// A <see cref="TimeProvider"/> whose <see cref="LocalTimeZone"/> is a given time zone instead of the time zone of the machine.
/// The clock, the timestamps and the timers come from an inner provider (<see cref="TimeProvider.System"/> by default).
/// </summary>
/// <remarks>
/// Use it to display or compute dates in the time zone of the business, whatever the time zone of the server or of the browser.
/// <see cref="TimeProvider.GetLocalNow"/> then returns the current time in that time zone.
/// Register it with <c>AddZonedTimeProvider</c>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var brussels = new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels"));
/// var now = brussels.GetLocalNow(); // 2026-10-29 11:00 +01:00 when it is 10:00 UTC
/// </code>
/// </example>
public sealed class ZonedTimeProvider : TimeProvider
{
    private readonly TimeProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZonedTimeProvider"/> class.
    /// </summary>
    /// <param name="timeZone">The time zone returned by <see cref="LocalTimeZone"/>.</param>
    /// <param name="inner">The provider of the clock and the timers. The default is <see cref="TimeProvider.System"/>; a <c>FakeTimeProvider</c> can be used in the tests.</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is <see langword="null"/>.</exception>
    public ZonedTimeProvider(TimeZoneInfo timeZone, TimeProvider? inner = null)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        LocalTimeZone = timeZone;
        _inner = inner ?? System;
    }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone { get; }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    /// <inheritdoc />
    public override long GetTimestamp() => _inner.GetTimestamp();

    /// <inheritdoc />
    public override long TimestampFrequency => _inner.TimestampFrequency;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => _inner.CreateTimer(callback, state, dueTime, period);
}
