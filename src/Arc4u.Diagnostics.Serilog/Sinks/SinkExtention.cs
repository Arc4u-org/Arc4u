using Arc4u.Diagnostics;
using Arc4u.Diagnostics.Sinks;
using Serilog.Configuration;
using Serilog.Core;

namespace Serilog;

/// <summary>Extension methods on <see cref="LoggerSinkConfiguration"/> for the Arc4u sinks.</summary>
public static class SinkExtention
{
    /// <summary>Writes to a sink only the events whose Arc4u message category is one of the given categories (see <see cref="CategoryFilterSink"/>).</summary>
    /// <remarks>
    /// The categories are <see cref="short"/> flags: a category invented by the application (<c>(MessageCategory)8</c>) is filtered like the predefined ones.
    /// </remarks>
    /// <param name="loggerConfiguration">The sink configuration (<c>WriteTo</c>).</param>
    /// <param name="category">The categories (flags) that are forwarded.</param>
    /// <param name="sink">The sink that receives the matching events.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    public static LoggerConfiguration CategoryFilter(this LoggerSinkConfiguration loggerConfiguration, MessageCategory category, ILogEventSink sink)
    {
        return loggerConfiguration.Sink(new CategoryFilterSink(category, sink));
    }

    /// <summary>Writes to a sink the events without their <see cref="LoggingConstants.Identity"/> property (see <see cref="AnonymizerSink"/>).</summary>
    /// <param name="loggerConfiguration">The sink configuration (<c>WriteTo</c>).</param>
    /// <param name="sink">The sink that receives the anonymized events.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    public static LoggerConfiguration Anonymizer(this LoggerSinkConfiguration loggerConfiguration, ILogEventSink sink)
    {
        return loggerConfiguration.Sink(new AnonymizerSink(sink));
    }
}
