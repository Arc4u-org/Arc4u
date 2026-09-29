namespace Arc4u;

/// <summary>
/// Helpers for <see cref="WaitHandle"/>.
/// </summary>
public class WaitHandleHelper
{
    /// <summary>
    /// Same as WaitHandle.WaitOne but accept a timeout with a value equal to Int64.MaxValue!
    /// </summary>
    /// <param name="waitHandle">The WaitHandle</param>
    /// <param name="timeout">The period</param>
    /// <param name="exitSync">true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it afterward; otherwise, false. </param>
    /// <returns>true if the current instance receives a signal; otherwise, false. </returns>
    public static bool WaitOne(WaitHandle waitHandle, TimeSpan timeout, bool exitSync)
    {
        if (timeout == TimeSpan.MaxValue)
        {
            waitHandle.WaitOne();
            return true;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        var maxWait = TimeSpan.FromMilliseconds(int.MaxValue);

        while (timeout > maxWait)
        {
            if (waitHandle.WaitOne(maxWait, exitSync))
            {
                return true;
            }
            timeout -= maxWait;
        }
        return waitHandle.WaitOne(timeout, exitSync);
    }
}
