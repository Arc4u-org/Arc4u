namespace Arc4u.Threading;

/// <summary>
/// An asynchronous mutual-exclusion lock that can be awaited (unlike <c>lock</c>, it can be held across an <c>await</c>).
/// It is not re-entrant: acquiring it twice from the same flow without releasing it deadlocks.
/// </summary>
/// <example>
/// <code language="csharp">
/// private readonly AsyncLock _lock = new();
///
/// public async Task UpdateAsync()
/// {
///     using (await _lock.LockAsync().ConfigureAwait(false))
///     {
///         // Only one caller at a time executes this block.
///         await Task.Delay(10).ConfigureAwait(false);
///     }
/// }
/// </code>
/// </example>
public class AsyncLock
{
    private readonly AsyncSemaphore m_semaphore;
    private readonly Task<Releaser> m_releaser;

    /// <summary>
    /// Initializes a new, unlocked instance of the <see cref="AsyncLock"/> class.
    /// </summary>
    public AsyncLock()
    {
        m_semaphore = new AsyncSemaphore(1);
        m_releaser = Task.FromResult(new Releaser(this));
    }

    /// <summary>
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <returns>A task that completes when the lock is acquired. Dispose the resulting <see cref="Releaser"/> to release the lock.</returns>
    public Task<Releaser> LockAsync()
    {
        var wait = m_semaphore.WaitAsync();
        return wait.IsCompleted ?
            m_releaser :
            wait.ContinueWith((_, state) => new Releaser((AsyncLock)state!),
                this, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// Releases the <see cref="AsyncLock"/> when disposed.
    /// </summary>
    public struct Releaser : IDisposable
    {
        private readonly AsyncLock m_toRelease;

        internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

        /// <summary>
        /// Releases the lock this instance was returned for.
        /// </summary>
        public void Dispose()
        {
            m_toRelease?.m_semaphore.Release();
        }
    }

}
