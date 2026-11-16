namespace Arc4u.Threading;

/// <summary>
/// The AsyncResult class is to perform asynchronous task. This is important when multiple asynchronous tasks are done in a ASP.Net page for sample.
/// </summary>
public class AsyncResult : IAsyncResult
{
    private readonly AsyncCallback _cb;
    private readonly object _state;
    private ManualResetEvent? _event;
    private bool _completed;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncResult"/> class.
    /// </summary>
    /// <param name="cb">The cb.</param>
    /// <param name="state">The state.</param>
    public AsyncResult(AsyncCallback cb, object state)
    {
        _cb = cb;
        _state = state;
    }

    /// <summary>
    /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
    /// </summary>
    /// <value></value>
    /// <returns>A user-defined object that qualifies or contains information about an asynchronous operation.</returns>
    public object AsyncState { get { return _state; } }

    /// <summary>
    /// Gets an indication of whether the asynchronous operation completed synchronously.
    /// </summary>
    /// <value></value>
    /// <returns>true if the asynchronous operation completed synchronously; otherwise, false.</returns>
    public bool CompletedSynchronously { get { return false; } }

    /// <summary>
    /// Gets an indication whether the asynchronous operation has completed.
    /// </summary>
    /// <value></value>
    /// <returns>true if the operation is complete; otherwise, false.</returns>
    public bool IsCompleted { get { return _completed; } }

    /// <summary>
    /// Gets a <see cref="T:System.Threading.WaitHandle"></see> that is used to wait for an asynchronous operation to complete.
    /// </summary>
    /// <value></value>
    /// <returns>A <see cref="T:System.Threading.WaitHandle"></see> that is used to wait for an asynchronous operation to complete.</returns>
    public WaitHandle AsyncWaitHandle
    {
        get
        {
            lock (_lock)
            {
                if (_event == null)
                {
                    _event = new ManualResetEvent(IsCompleted);
                }

                return _event;
            }
        }
    }

    /// <summary>
    /// Complete call must be called when all the asynchronous tasks are done and release the locked thread to perform the remaining work.
    /// </summary>
    public void CompleteCall()
    {
        lock (_lock)
        {
            _completed = true;
            _event?.Set();
        }

        if (_cb != null)
        {
            _cb(this);
        }
    }
}
