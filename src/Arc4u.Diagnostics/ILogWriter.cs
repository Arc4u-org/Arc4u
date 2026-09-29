namespace Arc4u.Diagnostics;

/// <summary>Represents a component that writes the log entries to a destination (for example a file or a database) and must be initialized before use.</summary>
public interface ILogWriter : IDisposable
{

    /// <summary>Initializes the writer and configures its underlying logging pipeline.</summary>
    void Initialize();
}
