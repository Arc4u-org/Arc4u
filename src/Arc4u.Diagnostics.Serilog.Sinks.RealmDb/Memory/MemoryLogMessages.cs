namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>The list of log messages held in memory by <see cref="MemoryLogDbSink"/> and read by <see cref="MemoryLogStore"/>.</summary>
public class MemoryLogMessages : List<LogMessage>
{
}
