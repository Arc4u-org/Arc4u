using Serilog.Core;
using Serilog.Events;

namespace Arc4u.Diagnostics.Sinks;

/// <summary>A sink that removes the <see cref="LoggingConstants.Identity"/> property from the events before forwarding them to another sink.</summary>
public class AnonymizerSink : ILogEventSink, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="AnonymizerSink"/> class.</summary>
    /// <param name="sink">The sink that receives the anonymized events.</param>
    public AnonymizerSink(ILogEventSink sink)
    {
        Sink = sink;
    }
    /// <summary>Gets or sets the sink that receives the anonymized events.</summary>
    public ILogEventSink Sink { get; set; }

    /// <summary>Disposes the wrapped sink when it is disposable.</summary>
    public void Dispose()
    {
        if (Sink is IDisposable)
        {
            ((IDisposable)Sink).Dispose();
        }
    }

    /// <summary>Forwards the event to the wrapped sink, without its <see cref="LoggingConstants.Identity"/> property.</summary>
    /// <param name="logEvent">The event to emit.</param>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(LoggingConstants.Identity, out var propertyValue))
        {
            var _event = new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties.Where(p => !p.Key.Equals(LoggingConstants.Identity)).Select(p => new LogEventProperty(p.Key, p.Value)).ToList());
            Sink?.Emit(_event);
        }
        else
        {
            Sink?.Emit(logEvent);
        }
    }
}
