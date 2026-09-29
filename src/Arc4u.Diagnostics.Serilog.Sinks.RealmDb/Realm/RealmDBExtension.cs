using Arc4u.Diagnostics.Serilog.Sinks.RealmDb;
using Realms;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog;

/// <summary>Extension methods to add the Realm log sink to a Serilog configuration.</summary>
public static class RealmDBExtension
{
    /// <summary>
    /// Writes the log events to a Realm database, configured by <see cref="DefaultConfig"/>, through a batching sink
    /// (batches of 50 events, every 500 ms, queue limit 10000).
    /// </summary>
    /// <param name="loggerConfiguration">The sink configuration.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// var configuration = new LoggerConfiguration();
    /// configuration.WriteTo.RealmDB();
    /// </code>
    /// </example>
    public static LoggerConfiguration RealmDB(this LoggerSinkConfiguration loggerConfiguration)
    {
        var sink = new RealmDBSink(DefaultConfig());

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

    /// <summary>Gets or sets the factory that creates the Realm configuration used by the sink. The default configuration uses the file <c>loggingDB.realm</c> with schema version 1.</summary>
    public static Func<RealmConfiguration> DefaultConfig = () => new RealmConfiguration("loggingDB.realm")
    {
        SchemaVersion = 1,
    };

}

