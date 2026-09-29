using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>Extension methods to add the in-memory log sink to a Serilog configuration.</summary>
public static class MemoryLogExtension
{
    /// <summary>
    /// Writes the log events to the in-memory store (<see cref="MemoryLogDbSink.LogMessages"/>) through a batching sink
    /// (batches of 50 events, every 500 ms, queue limit 10000).
    /// </summary>
    /// <param name="loggerConfiguration">The sink configuration.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// // Namespace: Arc4u.Diagnostics.Serilog.Sinks.Memory
    /// var configuration = new LoggerConfiguration();
    /// configuration.WriteTo.MemoryLogDB();
    /// </code>
    /// </example>
    public static LoggerConfiguration MemoryLogDB(this LoggerSinkConfiguration loggerConfiguration)
    {
        var sink = new MemoryLogDbSink();

        var batchingOptions = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = 50,
            Period = TimeSpan.FromMilliseconds(500),
            EagerlyEmitFirstEvent = true,
            QueueLimit = 10000
        };

        var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

        return loggerConfiguration.Sink(batchingSink);
    }

}
