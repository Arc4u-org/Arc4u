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

    /// <summary>Gets a page of the messages, in the order in which they were stored. When a criteria is given, only the messages whose text contains the criteria are returned (case-insensitive comparison).</summary>
    /// <param name="criteria">Text that the message must contain. When empty, no filter is applied.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to return.</param>
    /// <returns>The matching log messages.</returns>
    public List<LogMessage> GetLogs(string criteria, int skip, int take)
    {
        if (string.IsNullOrWhiteSpace(criteria))
        {
            return _logMessages.GetPage(null, skip, take);
        }

        return _logMessages.GetPage(m => m.Message?.Contains(criteria, StringComparison.OrdinalIgnoreCase) == true, skip, take);
    }

    /// <inheritdoc/>
    public void RemoveAll()
    {
        _logMessages.Clear();
    }
}
