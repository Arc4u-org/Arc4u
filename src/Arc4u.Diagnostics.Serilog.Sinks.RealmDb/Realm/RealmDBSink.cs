using Arc4u.Diagnostics.Formatter;
using Realms;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Arc4u.Diagnostics.Serilog.Sinks.RealmDb;

/// <summary>A batched Serilog sink that persists the log events in a Realm database as <see cref="LogDBMessage"/> objects.</summary>
public class RealmDBSink : IBatchedLogEventSink
{
    /// <summary>Initializes a new instance of the <see cref="RealmDBSink"/> class.</summary>
    /// <param name="config">The configuration of the Realm database.</param>
    public RealmDBSink(RealmConfiguration config)  //: base(50, TimeSpan.FromMilliseconds(500))
    {
        MessageFormatter = new MessageTemplateTextFormatter("{Message}");
        PropertyFormatter = new JsonPropertiesFormatter();
        Config = config;
    }

    private MessageTemplateTextFormatter MessageFormatter { get; set; }
    // Serialize in json the properties non Arc4u standard.
    private JsonPropertiesFormatter PropertyFormatter { get; set; }

    private RealmConfiguration Config { get; set; }

    /// <summary>Writes the events of the batch to the Realm database in one transaction. Events that cannot be converted are ignored.</summary>
    /// <param name="events">The batch of events.</param>
    /// <returns>A task that completes when the batch has been written.</returns>
    public Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        var messages = new List<LogDBMessage>();
        foreach (var _event in events)
        {
            try
            {
                messages.Add(ToLogDBMessage(_event));
            }
            catch (Exception)
            {
            }
        }

        if (messages.Count == 0)
        {
            return Task.CompletedTask;
        }

        // A Realm instance can only be used on the thread that opened it, and Realm binds it to the current synchronization context.
        // The instance is opened, used and closed synchronously without one, so it never moves to another thread and its native handles are released.
        var current = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            using var realm = Realm.GetInstance(Config);
            realm.Write(() =>
            {
                foreach (var message in messages)
                {
                    realm.Add(message);
                }
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }

        return Task.CompletedTask;
    }

    private LogDBMessage ToLogDBMessage(LogEvent _event)
    {
        var (Category, Application, Identity, ClassType, MethodName, ActivityId, ProcessId, ThreadId, Stacktrace, Properties) = Helper.ExtractEventInfo(_event);

        using var properties = new StringWriter();
        using var messageText = new StringWriter();
        PropertyFormatter.Format(Properties, properties);
        MessageFormatter.Format(_event, messageText);

        return new LogDBMessage
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
            MessageCategory = (short)Category,
            MessageType = (int)_event.Level.ToMessageType(),
            Application = Application,
            Identity = Identity,
        };
    }

    /// <summary>Called when no event is available; does nothing.</summary>
    /// <returns>A completed task.</returns>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}
