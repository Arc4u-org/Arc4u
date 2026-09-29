namespace Arc4u.Threading;

/// <summary>
/// A minimal asynchronous semaphore: callers wait with <see cref="WaitAsync"/> without blocking a thread and are served in first-in first-out order.
/// </summary>
public class AsyncSemaphore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncSemaphore"/> class.
    /// </summary>
    /// <param name="initialCount">The number of callers that can enter without waiting.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is negative.</exception>
    public AsyncSemaphore(int initialCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCount);

        m_currentCount = initialCount;
    }

    /// <summary>
    /// Asynchronously waits to enter the semaphore.
    /// </summary>
    /// <returns>A task that is already completed when a slot is available; otherwise a task that completes when another caller calls <see cref="Release"/>.</returns>
    public Task WaitAsync()
    {
        lock (m_waiters)
        {
            if (m_currentCount > 0)
            {
                --m_currentCount;
                return s_completed;
            }
            else
            {
                var waiter = new TaskCompletionSource<bool>();
                m_waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }
    }
    /// <summary>
    /// Releases the semaphore: the oldest waiter, if any, is resumed; otherwise the available count is incremented.
    /// </summary>
    public void Release()
    {
        TaskCompletionSource<bool>? toRelease = null;
        lock (m_waiters)
        {
            if (m_waiters.Count > 0)
            {
                toRelease = m_waiters.Dequeue();
            }
            else
            {
                ++m_currentCount;
            }
        }
        toRelease?.SetResult(true);
    }

    private static readonly Task s_completed = Task.FromResult(true);
    private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
    private int m_currentCount;
}
