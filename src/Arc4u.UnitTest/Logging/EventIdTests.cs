using Arc4u.Dependency;
using Arc4u.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public partial class EventIdTests
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Order {OrderId} shipped")]
    private static partial void LogOrderShipped(ILogger logger, int orderId);

    private static (ILogger<EventIdTests> Logger, EventIdCapture Capture) CreateLogger()
    {
        var capture = new EventIdCapture();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
        services.AddILogger();

        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<EventIdTests>>();
        return (logger, capture);
    }

    [Fact]
    public void Log_With_EventId_Should_Forward_It()
    {
        var (logger, capture) = CreateLogger();

        logger.Technical().Log(LogLevel.Information, new EventId(42, "Answer"), "Message");

        capture.EventIds.Should().ContainSingle().Which.Should().Be(new EventId(42, "Answer"));
    }

    [Fact]
    public void LoggerMessage_EventId_Should_Be_Forwarded()
    {
        var (logger, capture) = CreateLogger();

        LogOrderShipped(logger.Business().Add("Carrier", "dhl"), 7);

        capture.EventIds.Should().ContainSingle().Which.Id.Should().Be(1001);
    }

    [Fact]
    public void AggregateException_Entries_Should_Keep_The_EventId()
    {
        var (logger, capture) = CreateLogger();

        logger.Technical().LogError(new EventId(7), new AggregateException(new InvalidOperationException(), new ArgumentException()), "Failed");

        capture.EventIds.Should().HaveCount(3).And.OnlyContain(e => e.Id == 7);
    }

    private sealed class EventIdCapture : ILoggerProvider, ILogger
    {
        public List<EventId> EventIds { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => EventIds.Add(eventId);

        public void Dispose() { }
    }
}
