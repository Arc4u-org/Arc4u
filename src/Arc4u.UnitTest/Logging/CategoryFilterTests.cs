using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.Diagnostics.Formatter;
using Arc4u.Diagnostics.Serilog;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class CategoryFilterTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static ILogger<CategoryFilterTests> BuildLogger(MessageCategory categories, CollectingSink filtered, CollectingSink all)
    {
        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.CategoryFilter(categories, filtered)
            .WriteTo.Sink(all)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(serilog, false));
        services.AddILogger();

        return services.BuildServiceProvider().GetRequiredService<ILogger<CategoryFilterTests>>();
    }

    [Fact]
    public void CategoryFilter_Forwards_All_Events_When_All_Categories_Are_Enabled()
    {
        var filtered = new CollectingSink();
        var logger = BuildLogger(MessageCategory.Technical | MessageCategory.Business | MessageCategory.Monitoring, filtered, new CollectingSink());

        logger.Technical().LogInformation("technical");
        logger.Business().LogInformation("business");
        logger.Monitoring().LogInformation("monitoring");

        filtered.Events.Should().HaveCount(3);
    }

    [Fact]
    public void CategoryFilter_Forwards_Only_The_Configured_Categories()
    {
        var filtered = new CollectingSink();
        var logger = BuildLogger(MessageCategory.Business | MessageCategory.Monitoring, filtered, new CollectingSink());

        logger.Technical().LogInformation("technical");
        logger.Business().LogInformation("business");
        logger.Monitoring().LogInformation("monitoring");

        filtered.Events.Select(e => e.MessageTemplate.Text).Should().Equal("business", "monitoring");
    }

    [Fact]
    public void ExtractEventInfo_Returns_The_Logged_Category()
    {
        var all = new CollectingSink();
        var logger = BuildLogger(MessageCategory.Technical, new CollectingSink(), all);

        logger.Technical().LogInformation("technical");
        logger.Business().LogInformation("business");
        logger.Monitoring().LogInformation("monitoring");

        all.Events.Select(e => Helper.ExtractEventInfo(e).Category)
           .Should().Equal(MessageCategory.Technical, MessageCategory.Business, MessageCategory.Monitoring);
    }

    [Fact]
    public void CategoryFilter_Forwards_A_Category_Invented_By_The_Application()
    {
        const MessageCategory Security = (MessageCategory)8;
        var filtered = new CollectingSink();
        var all = new CollectingSink();
        var logger = BuildLogger(MessageCategory.Business | Security, filtered, all);

        var wrapper = (ILoggerWrapper<CategoryFilterTests>)logger;
        wrapper.SetContext(Security.ToString()).LogInformation("security");
        logger.Technical().LogInformation("technical");

        filtered.Events.Select(e => e.MessageTemplate.Text).Should().Equal("security");
        Helper.ExtractEventInfo(all.Events[0]).Category.Should().Be(Security);
    }

    [Fact]
    public void SimpleTextFormatter_Writes_A_Category_Invented_By_The_Application()
    {
        var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, MessageTemplate.Empty,
                                    [new LogEventProperty(LoggingConstants.Category, new ScalarValue("512"))]);
        using var output = new StringWriter();

        new SimpleTextFormatter().Format(logEvent, output);

        output.ToString().Should().Contain(" 512 ");
    }

    [Fact]
    public void A_Named_Category_Is_Written_Filtered_And_Read_By_Its_Name()
    {
        var filtered = new CollectingSink();
        var all = new CollectingSink();
        var logger = BuildLogger(AuditLoggerExtensions.AuditCategory, filtered, all);

        logger.Audit().LogInformation("audit");
        logger.Technical().LogInformation("technical");

        filtered.Events.Select(e => e.MessageTemplate.Text).Should().Equal("audit");
        all.Events[0].Properties[LoggingConstants.Category].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("Audit");
        all.Events[0].Properties[LoggingConstants.Class].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(typeof(CategoryFilterTests).FullName);
        Helper.ExtractEventInfo(all.Events[0]).Category.Should().Be(AuditLoggerExtensions.AuditCategory);

        using var output = new StringWriter();
        new SimpleTextFormatter().Format(all.Events[0], output);
        output.ToString().Should().Contain(" Audit ").And.Contain(nameof(A_Named_Category_Is_Written_Filtered_And_Read_By_Its_Name));
    }

    [Theory]
    [InlineData(MessageCategory.Business, MessageCategory.Business)]
    [InlineData("Monitoring", MessageCategory.Monitoring)]
    [InlineData("business", MessageCategory.Business)]
    [InlineData((short)4, MessageCategory.Monitoring)]
    [InlineData(2, MessageCategory.Business)]
    [InlineData("8", (MessageCategory)8)]
    [InlineData(16, (MessageCategory)16)]
    [InlineData(3, MessageCategory.Technical | MessageCategory.Business)]
    public void GetCategory_Accepts_Enum_Name_And_Number(object value, MessageCategory expected)
    {
        Helper.GetCategory(new ScalarValue(value)).Should().Be(expected);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(40000)]
    [InlineData(null)]
    public void GetCategory_Returns_Null_For_A_Value_That_Is_Not_A_Category(object? value)
    {
        Helper.GetCategory(new ScalarValue(value)).Should().BeNull();
    }
}

/// <summary>The pattern a developer follows to add a named category.</summary>
public static class AuditLoggerExtensions
{
    public const MessageCategory AuditCategory = (MessageCategory)256;

    static AuditLoggerExtensions() => MessageCategoryRegistry.Register("Audit", AuditCategory);

    public static ILoggerWrapper<T> Audit<T>(this ILogger<T> logger, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "") =>
        logger.Category(AuditCategory, methodName);
}
