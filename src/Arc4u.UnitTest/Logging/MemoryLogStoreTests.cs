using Arc4u.Diagnostics.Serilog.Sinks;
using Arc4u.Diagnostics.Serilog.Sinks.Memory;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class MemoryLogStoreTests
{
    private static (MemoryLogMessages, MemoryLogStore) BuildStore(params string?[] messages)
    {
        var logMessages = new MemoryLogMessages();
        foreach (var message in messages)
        {
            logMessages.Add(new LogMessage { Message = message! });
        }

        return (logMessages, new MemoryLogStore(logMessages));
    }

    [Theory]
    [InlineData("order")]
    [InlineData("shipped")]
    [InlineData("Shipped")]
    [InlineData("ORDER SHIPPED")]
    [InlineData("Order Shipped")]
    public void GetLogs_Matches_Criteria_Without_Regard_To_Case(string criteria)
    {
        var (_, store) = BuildStore("Order Shipped", "Payment received");

        var logs = store.GetLogs(criteria, 0, 10);

        logs.Should().ContainSingle().Which.Message.Should().Be("Order Shipped");
    }

    [Fact]
    public void GetLogs_Returns_Nothing_When_No_Message_Contains_Criteria()
    {
        var (_, store) = BuildStore("Order Shipped", "Payment received");

        store.GetLogs("refund", 0, 10).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GetLogs_Without_Criteria_Returns_A_Page_In_Insertion_Order(string? criteria)
    {
        var (_, store) = BuildStore("first", "second", "third", "fourth");

        var logs = store.GetLogs(criteria!, 1, 2);

        logs.Select(m => m.Message).Should().Equal("second", "third");
    }

    [Fact]
    public void GetLogs_With_Criteria_Skips_And_Takes_Among_Matching_Messages()
    {
        var (_, store) = BuildStore("Order 1", "Payment 1", "order 2", "Payment 2", "ORDER 3", "order 4");

        var logs = store.GetLogs("order", 1, 2);

        logs.Select(m => m.Message).Should().Equal("order 2", "ORDER 3");
    }

    [Fact]
    public void GetLogs_Ignores_Messages_Without_Text()
    {
        var (_, store) = BuildStore(null, "Order Shipped");

        store.GetLogs("order", 0, 10).Should().ContainSingle();
    }

    [Fact]
    public void GetLogs_Returns_A_Copy_That_Does_Not_Change_With_The_Store()
    {
        var (logMessages, store) = BuildStore("first");

        var logs = store.GetLogs("", 0, 10);
        logMessages.Add(new LogMessage { Message = "second" });
        store.RemoveAll();

        logs.Should().ContainSingle().Which.Message.Should().Be("first");
    }

    [Fact]
    public void RemoveAll_Clears_The_Messages()
    {
        var (logMessages, store) = BuildStore("first", "second");

        store.RemoveAll();

        logMessages.Count.Should().Be(0);
        store.GetLogs("", 0, 10).Should().BeEmpty();
    }

    [Fact]
    public void Enumerating_The_Messages_Walks_A_Snapshot()
    {
        var (logMessages, _) = BuildStore("first", "second");

        var read = new List<string>();
        foreach (var message in logMessages)
        {
            read.Add(message.Message);
            logMessages.Add(new LogMessage { Message = "added while enumerating" });
        }

        read.Should().Equal("first", "second");
        logMessages.Count.Should().Be(4);
    }

    [Fact]
    public async Task Reading_While_Messages_Are_Added_Does_Not_Throw()
    {
        var (logMessages, store) = BuildStore();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var writer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                logMessages.Add(new LogMessage { Message = "Order Shipped" });
            }
        });

        var reader = Task.Run(() =>
        {
            var reads = 0;
            while (!cts.IsCancellationRequested)
            {
                store.GetLogs("order", 0, 1000);
                foreach (var _ in logMessages)
                {
                }
                if (++reads % 50 == 0)
                {
                    store.RemoveAll();
                }
            }
        });

        var act = () => Task.WhenAll(writer, reader);

        await act.Should().NotThrowAsync();
    }
}
