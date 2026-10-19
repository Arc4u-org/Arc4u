using Arc4u.Dependency;
using Arc4u.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arc4u.UnitTest.Logging;

/// <summary>
/// The properties added to a log entry must not be written on later entries (issue #273).
/// </summary>
[Trait("Category", "CI")]
public class EntryPropertiesTests
{
    private static (ILogger<EntryPropertiesTests> Logger, PropertiesCapture Capture) CreateLogger()
    {
        var capture = new PropertiesCapture();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
        services.AddILogger();

        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<EntryPropertiesTests>>();
        return (logger, capture);
    }

    [Fact]
    public void Added_Property_Should_Not_Be_Written_On_The_Next_Entry()
    {
        var (logger, capture) = CreateLogger();

        logger.Technical().Add("OrderId", 42).LogInformation("First");
        logger.Technical().LogInformation("Second");
        logger.LogInformation("Third");

        capture.Entries[0].Should().ContainKey("OrderId");
        capture.Entries[1].Should().NotContainKey("OrderId");
        capture.Entries[2].Should().NotContainKey("OrderId");
    }

    [Fact]
    public void Template_Argument_Should_Not_Be_Written_On_The_Next_Entry()
    {
        var (logger, capture) = CreateLogger();

        logger.LogInformation("Order {OrderId}", 42);
        logger.Technical().LogError(new InvalidOperationException(), "Customer {Customer}", "contoso");
        logger.Business().LogInformation("Done");

        capture.Entries[0].Should().ContainKey("OrderId").And.NotContainKey("Customer");
        capture.Entries[1].Should().ContainKey("Customer").And.NotContainKey("OrderId");
        capture.Entries[2].Should().NotContainKeys("OrderId", "Customer");
    }

    [Fact]
    public void Kept_Entry_Logger_Should_Start_Each_Entry_Without_Properties()
    {
        var (logger, capture) = CreateLogger();

        var entry = logger.Technical();
        entry.Add("OrderId", 42).AddStackTrace().LogInformation("First");
        entry.LogInformation("Second");

        capture.Entries[0].Should().ContainKeys("OrderId", LoggingConstants.Stacktrace);
        capture.Entries[1].Should().NotContainKeys("OrderId", LoggingConstants.Stacktrace);
    }

    [Fact]
    public void Category_Should_Not_Be_Kept_On_The_Shared_Logger()
    {
        var (logger, capture) = CreateLogger();

        logger.Business().LogInformation("Business");
        logger.LogInformation("Plain");

        capture.Entries[0][LoggingConstants.Category].Should().Be(nameof(MessageCategory.Business));
        capture.Entries[1][LoggingConstants.Category].Should().Be(nameof(MessageCategory.Technical));
    }

    [Fact]
    public void Concurrent_Entries_Should_Only_Carry_Their_Own_Properties()
    {
        var (logger, capture) = CreateLogger();

        Parallel.For(0, 1000, i => logger.Technical().Add("Index", i).LogInformation("Entry {Message}", i));

        capture.Entries.Should().HaveCount(1000)
                       .And.OnlyContain(e => Equals(e["Index"], e["Message"]));
    }

    private sealed class PropertiesCapture : ILoggerProvider, ILogger
    {
        private readonly object _lock = new();

        public List<Dictionary<string, object?>> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                lock (_lock)
                {
                    Entries.Add(properties.ToDictionary(p => p.Key, p => p.Value));
                }
            }
        }

        public void Dispose() { }
    }
}
