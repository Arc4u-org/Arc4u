namespace Arc4u.Diagnostics.Serilog.Sinks;

/// <summary>Represents a store of log messages that can be queried and cleared, for example to display the logs of an application.</summary>
public interface ILogStore
{
    /// <summary>Gets a page of the stored log messages.</summary>
    /// <param name="criteria">Text that the message must contain. When <see langword="null"/>, empty or white space, no filter is applied.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to read.</param>
    /// <returns>The matching log messages.</returns>
    List<LogMessage> GetLogs(string criteria, int skip, int take);

    /// <summary>Removes all the stored log messages.</summary>
    void RemoveAll();
}
