using System.Collections;

namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>The thread-safe collection of log messages held in memory by <see cref="MemoryLogDbSink"/> and read by <see cref="MemoryLogStore"/>.</summary>
/// <remarks>The sink adds messages on a background thread. Every member takes an internal lock, and enumerating the collection walks a snapshot taken when the enumeration starts.</remarks>
public class MemoryLogMessages : IReadOnlyCollection<LogMessage>
{
    private readonly List<LogMessage> _messages = [];
    private readonly object _lock = new();

    /// <summary>Gets the number of messages.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _messages.Count;
            }
        }
    }

    /// <summary>Adds a message at the end of the collection.</summary>
    /// <param name="message">The message to add.</param>
    public void Add(LogMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    /// <summary>Removes all the messages.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }

    /// <summary>Gets a page of the messages that match a predicate, in the order in which they were added.</summary>
    /// <param name="predicate">The filter to apply, or <c>null</c> to keep all the messages.</param>
    /// <param name="skip">The number of matching messages to skip.</param>
    /// <param name="take">The maximum number of messages to return.</param>
    /// <returns>A new list with the matching messages.</returns>
    public List<LogMessage> GetPage(Func<LogMessage, bool>? predicate, int skip, int take)
    {
        lock (_lock)
        {
            IEnumerable<LogMessage> messages = predicate is null ? _messages : _messages.Where(predicate);
            return messages.Skip(skip).Take(take).ToList();
        }
    }

    /// <summary>Returns an enumerator over a snapshot of the messages.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<LogMessage> GetEnumerator()
    {
        List<LogMessage> snapshot;
        lock (_lock)
        {
            snapshot = [.. _messages];
        }
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
