using Arc4u.Diagnostics.Formatter;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Arc4u.Diagnostics.Serilog.Sinks.Memory;

/// <summary>A batched Serilog sink that keeps the log events in memory, in the static <see cref="LogMessages"/> collection.</summary>
public class MemoryLogDbSink : IBatchedLogEventSink
{
    /// <summary>Initializes a new instance of the <see cref="MemoryLogDbSink"/> class.</summary>
    public MemoryLogDbSink() //: base(50, TimeSpan.FromMilliseconds(500))
    {
        MessageFormatter = new MessageTemplateTextFormatter("{Message}");
        PropertyFormatter = new JsonPropertiesFormatter();

    }
    private MessageTemplateTextFormatter MessageFormatter { get; set; }
    // Serialize in json the properties non Arc4u standard.
    private JsonPropertiesFormatter PropertyFormatter { get; set; }

    /// <summary>Gets the in-memory collection, shared by all the instances, that receives the log messages.</summary>
    public static MemoryLogMessages LogMessages { get; } = [];

    /// <summary>Converts each event of the batch to a <see cref="LogMessage"/> and adds it to <see cref="LogMessages"/>. Events that cannot be converted are ignored.</summary>
    /// <param name="events">The batch of events.</param>
    /// <returns>A completed task.</returns>
    public Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        foreach (var _event in events)
        {
            var (Category, Application, Identity, ClassType, MethodName, ActivityId, ProcessId, ThreadId, Stacktrace, Properties) = Helper.ExtractEventInfo(_event);

            try
            {
                using var properties = new StringWriter();
                using var messageText = new StringWriter();
                PropertyFormatter.Format(Properties, properties);
                MessageFormatter.Format(_event, messageText);

                var logMsg = new LogMessage
                {
                    Timestamp = _event.Timestamp,
                    Message = messageText.ToString(),
                    ActivityId = ActivityId,
                    MethodName = MethodName,
                    ClassType = ClassType,
                    Properties = properties.ToString(),
                    ProcessId = ProcessId,
                    ThreadId = ThreadId,
                    Stacktrace = Stacktrace,
                    MessageCategory = Category.ToString(),
                    MessageType = _event.Level.ToMessageType().ToString(),
                    Application = Application,
                    Identity = Identity,
                };

                LogMessages.Add(logMsg);
            }
            catch (Exception)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Called when no event is available; does nothing.</summary>
    /// <returns>A completed task.</returns>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}
