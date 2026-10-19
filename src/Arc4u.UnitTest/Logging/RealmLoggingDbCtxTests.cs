using Arc4u.Diagnostics;
using Arc4u.Diagnostics.Serilog.Sinks;
using Arc4u.Diagnostics.Serilog.Sinks.RealmDb;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Realms;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public sealed class RealmLoggingDbCtxTests : IDisposable
{
    private readonly RealmConfiguration _config = new(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.realm"))
    {
        SchemaVersion = 1,
    };

    public void Dispose()
    {
        Realm.DeleteRealm(_config);
    }

    private void Seed(params string[] messages)
    {
        // Realm does not support the xUnit synchronization context, which is not bound to one thread.
        var current = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            Write(messages);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    private void Write(string[] messages)
    {
        using var realm = Realm.GetInstance(_config);
        var timestamp = DateTimeOffset.UtcNow;
        realm.Write(() =>
        {
            foreach (var message in messages)
            {
                timestamp = timestamp.AddSeconds(1);
                realm.Add(new LogDBMessage
                {
                    Timestamp = timestamp,
                    Message = message,
                    MessageCategory = (short)MessageCategory.Technical,
                    MessageType = (int)LogLevel.Information,
                });
            }
        });
    }

    [Fact]
    public async Task Singleton_Store_Can_Be_Used_From_Other_Threads()
    {
        Seed("first", "second", "third");

        using var provider = new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton<ILogStore, RealmLoggingDbCtx>()
            .BuildServiceProvider();

        // Resolve on a dedicated thread, then use it from other threads, as an ASP.NET Core request would.
        var store = await Task.Factory.StartNew(() => provider.GetRequiredService<ILogStore>(), TaskCreationOptions.LongRunning);

        var logs = await Task.Factory.StartNew(() => store.GetLogs("", 0, 10), TaskCreationOptions.LongRunning);
        logs.Select(l => l.Message).Should().Equal("third", "second", "first");

        await Task.Factory.StartNew(store.RemoveAll, TaskCreationOptions.LongRunning);

        var afterClear = await Task.Factory.StartNew(() => store.GetLogs("", 0, 10), TaskCreationOptions.LongRunning);
        afterClear.Should().BeEmpty();
    }

    [Fact]
    public void GetLogs_Filters_On_Criteria_Without_Regard_To_Case()
    {
        Seed("Order Shipped", "Payment received");

        var store = new RealmLoggingDbCtx(_config);

        store.GetLogs("SHIPPED", 0, 10).Should().ContainSingle().Which.Message.Should().Be("Order Shipped");
    }
}
