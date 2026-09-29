using System.Runtime.InteropServices;

namespace Arc4u;

/// <summary>
/// Measures a timeout from the moment it is created and provides helpers to compute with time spans where <see cref="TimeSpan.MaxValue"/> means "infinite".
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TimeoutHelper
{
    private readonly DateTime deadline;
    private readonly TimeSpan originalTimeout;

    /// <summary>
    /// Define a period of time.
    /// </summary>
    /// <param name="timeout">The period.</param>
    public TimeoutHelper(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        originalTimeout = timeout;

        if (timeout == TimeSpan.MaxValue)
        {
            deadline = DateTime.MaxValue;
        }
        else
        {
            deadline = DateTime.UtcNow + timeout;
        }
    }

    /// <summary>
    /// Get the period defined.
    /// </summary>
    public TimeSpan OriginalTimeout
    {
        get
        {
            return originalTimeout;
        }
    }

    /// <summary>
    /// Get the time remaining from since the object is instantiated.
    /// </summary>
    /// <returns></returns>
    public TimeSpan RemainingTime()
    {
        if (deadline == DateTime.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        var remaining = deadline - DateTime.UtcNow;

        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>
    /// Execute the callback method when the period is elapsed or immediately if the period is aleady elapsed.
    /// </summary>
    /// <param name="callback">The method to call.</param>
    /// <param name="state">Additional parameter.</param>
    public Timer SetTimer(TimerCallback callback, object? state)
    {
        return new Timer(callback, state, ToMilliseconds(RemainingTime()), -1);
    }

    /// <summary>
    /// Compute a TimeSpan from an integer but gives infinite value if equal to -1.
    /// </summary>
    /// <param name="milliseconds">period.</param>
    /// <returns>The computed TimeSpan.</returns>
    public static TimeSpan FromMilliseconds(int milliseconds)
    {
        if (milliseconds == -1)
        {
            return TimeSpan.MaxValue;
        }
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Compute a TimeSpan from an unsigned integer but gives infinite value if equal to MaxValue.
    /// </summary>
    /// <param name="milliseconds">period.</param>
    /// <returns>The computed TimeSpan.</returns>
    public static TimeSpan FromMilliseconds(uint milliseconds)
    {
        if (milliseconds == uint.MaxValue)
        {
            return TimeSpan.MaxValue;
        }
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Returns the number of ticks from the TimeSpan in integer..
    /// </summary>
    /// <param name="timeout">The TimeSpan</param>
    /// <returns></returns>
    public static int ToMilliseconds(TimeSpan timeout)
    {
        if (timeout == TimeSpan.MaxValue)
        {
            return -1;
        }

        var ticks = Ticks.FromTimeSpan(timeout);

        return (ticks / 0x2710L) > 0x7fffffffL ? 0x7fffffff : Ticks.ToMilliseconds(ticks);
    }

    /// <summary>
    /// Get the sum of 2 TimeSpan.
    /// </summary>
    /// <param name="timeout1">First TimeSpan</param>
    /// <param name="timeout2">Second TimeSpan</param>
    /// <returns>TimeSpan1 + TimeSpan2.</returns>
    public static TimeSpan Add(TimeSpan timeout1, TimeSpan timeout2)
    {
        return Ticks.ToTimeSpan(Ticks.Add(Ticks.FromTimeSpan(timeout1), Ticks.FromTimeSpan(timeout2)));
    }

    /// <summary>
    /// Add to the DateTime the TimeSpan.
    /// </summary>
    /// <param name="time">The DateTime</param>
    /// <param name="timeout">The period.</param>
    /// <returns>The sum.</returns>
    public static DateTime Add(DateTime time, TimeSpan timeout)
    {
        if ((timeout >= TimeSpan.Zero) && ((DateTime.MaxValue - time) <= timeout))
        {
            return DateTime.MaxValue;
        }
        if ((timeout <= TimeSpan.Zero) && ((DateTime.MinValue - time) >= timeout))
        {
            return DateTime.MinValue;
        }
        return (time + timeout);
    }

    /// <summary>
    /// Substract from the DateTime the period.
    /// </summary>
    /// <param name="time">The DateTime</param>
    /// <param name="timeout">The period</param>
    /// <returns>The substraction.</returns>
    public static DateTime Subtract(DateTime time, TimeSpan timeout)
    {
        return Add(time, TimeSpan.Zero - timeout);
    }

    /// <summary>
    /// Divide the period by the factor.
    /// </summary>
    /// <param name="timeout">The period.</param>
    /// <param name="factor">The factor.</param>
    /// <returns>The period / the factor.</returns>
    public static TimeSpan Divide(TimeSpan timeout, int factor)
    {
        if (timeout == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }
        return Ticks.ToTimeSpan((Ticks.FromTimeSpan(timeout) / factor));
    }
}

