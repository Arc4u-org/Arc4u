using Arc4u.Diagnostics;
using Arc4u.Diagnostics.Serilog.Sinks.RealmDb;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Realms;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public sealed class RealmDBSinkTests : IDisposable
{
    private readonly RealmConfiguration _config = new(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.realm"))
    {
        SchemaVersion = 1,
    };

    public void Dispose()
    {
        Realm.DeleteRealm(_config);
    }

    private static LogEvent Event(string message, LogEventLevel level = LogEventLevel.Information)
    {
        return new LogEvent(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse(message),
                            [new LogEventProperty(LoggingConstants.Category, new ScalarValue(MessageCategory.Business.ToString()))]);
    }

    private List<(string Message, short Category, int Type)> ReadAll()
    {
        // Realm does not support the xUnit synchronization context, which is not bound to one thread.
        var current = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            using var realm = Realm.GetInstance(_config);
            return realm.All<LogDBMessage>().ToList().Select(m => (m.Message, m.MessageCategory, m.MessageType)).OrderBy(m => m.Message).ToList();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    [Fact]
    public async Task EmitBatchAsync_Writes_The_Batch_And_Closes_The_Realm()
    {
        var sink = new RealmDBSink(_config);

        // Called under the xUnit synchronization context, which Realm does not support.
        await sink.EmitBatchAsync([Event("first"), Event("second", LogEventLevel.Warning)]);

        ReadAll().Should().Equal(("first", (short)MessageCategory.Business, (int)LogLevel.Information),
                                 ("second", (short)MessageCategory.Business, (int)LogLevel.Warning));

        // Deleting the file throws while an instance is still open.
        var delete = () => Realm.DeleteRealm(_config);
        delete.Should().NotThrow();
    }

    [Fact]
    public async Task EmitBatchAsync_Can_Be_Called_From_Different_Threads()
    {
        var sink = new RealmDBSink(_config);

        for (var batch = 0; batch < 5; batch++)
        {
            var events = Enumerable.Range(0, 10).Select(i => Event($"batch {batch} event {i}")).ToList();
            await Task.Factory.StartNew(() => sink.EmitBatchAsync(events), TaskCreationOptions.LongRunning).Unwrap();
        }

        ReadAll().Should().HaveCount(50);
    }
}
