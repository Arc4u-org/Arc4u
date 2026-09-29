namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>An <see cref="ILogStore"/> over the log messages kept in memory.</summary>
public class MemoryLogStore : ILogStore
{
    /// <summary>Initializes a new instance of the <see cref="MemoryLogStore"/> class.</summary>
    /// <param name="logMessages">The in-memory log messages to read.</param>
    public MemoryLogStore(MemoryLogMessages logMessages)
    {
        _logMessages = logMessages;
    }

    private readonly MemoryLogMessages _logMessages;

    /// <summary>Gets a page of the messages, in the order in which they were stored. When a criteria is given, only the messages whose text contains the lower-cased criteria are returned (case-sensitive comparison).</summary>
    /// <param name="criteria">Text that the message must contain. When empty, no filter is applied.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to return.</param>
    /// <returns>The matching log messages.</returns>
    public List<LogMessage> GetLogs(string criteria, int skip, int take)
    {
        var hasCriteria = !string.IsNullOrWhiteSpace(criteria);
        var searchText = hasCriteria ? criteria.ToLowerInvariant() : "";

        if (hasCriteria)
        {
            return _logMessages.Where(m => m.Message.Contains(searchText)).Skip(skip).Take(take).ToList();
        }

        return _logMessages.Skip(skip).Take(take).ToList();
    }

    /// <inheritdoc/>
    public void RemoveAll()
    {
        _logMessages.Clear();
    }
}
