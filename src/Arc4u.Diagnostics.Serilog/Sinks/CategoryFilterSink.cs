using Serilog.Core;
using Serilog.Events;

namespace Arc4u.Diagnostics.Sinks;

/// <summary>A sink that forwards to another sink only the events whose Arc4u <see cref="MessageCategory"/> is one of the configured categories.</summary>
public class CategoryFilterSink : ILogEventSink, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="CategoryFilterSink"/> class.</summary>
    /// <param name="categories">The categories (flags) that are forwarded.</param>
    /// <param name="sink">The sink that receives the matching events.</param>
    public CategoryFilterSink(MessageCategory categories, ILogEventSink sink)
    {
        Categories = categories;
        Sink = sink;
    }

    private MessageCategory Categories { get; set; }
    /// <summary>Gets or sets the sink that receives the matching events.</summary>
    public ILogEventSink Sink { get; set; }

    /// <summary>Disposes the wrapped sink when it is disposable.</summary>
    public void Dispose()
    {
        if (Sink is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Check if the message contains a category property!
    /// If no, the message is skipped.
    /// If yes, only messages with the registered categories are sent.
    /// </summary>
    /// <param name="logEvent">The event to filter.</param>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(LoggingConstants.Category, out var propertyValue))
        {
            var iCategory = Helper.GetValue<short>(propertyValue, -1);
            if (typeof(MessageCategory).IsEnumDefined(iCategory))
            {
                var category = (MessageCategory)iCategory;
                if (Categories.HasFlag(category))
                {
                    Sink?.Emit(logEvent);
                }
            }
        }
    }
}
