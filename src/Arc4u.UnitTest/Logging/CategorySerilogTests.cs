using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.Diagnostics.Monitoring;
using Arc4u.Diagnostics.Serilog;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class CategorySerilogTesters
{
    [Fact]
    public void LoggerTechnicalTest()
    {
        var services = new ServiceCollection();

        var sink = new SinkTest();

        var serilog = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .MinimumLevel.Debug()
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog, false));
        services.AddILogger();

        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<CategorySerilogTesters>>()!;

        sink.Emited = false;
        logger.Technical().Add("Code", "100").LogDebug("Technical");
        sink.Emited.Should().BeTrue();

        logger.Business().Add("Code", "100").Add("Code", "100").LogDebug("Business");
        sink.Emited.Should().BeTrue();
        sink.Emited = false;

        logger.Monitoring().Add("Code", "100").AddMemoryUsage().LogDebug("Monitoring");
        sink.Emited.Should().BeTrue();
    }

    [Fact]
    public async Task LoggerSystemResourcesTest()
    {
        // Register Serilog.
        var services = new ServiceCollection();

        var sink = new SinkTest();

        var serilog = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog, false));
        services.AddILogger();

        var serviceProvider = services.BuildServiceProvider();

#pragma warning disable CS0618 // SystemResources is obsolete: keep testing it until it is removed.
        var logger = serviceProvider.GetService<ILogger<SystemResources>>()!;
#pragma warning restore CS0618

        logger.Monitoring().Add("Code", 100).LogInformation("Message monitoring");

        Assert.True(sink.Emited);
        sink.Emited = false;
    }
}

public class LoggerCategorySinkTest : SerilogWriter
{
    public SinkTest? Sink { get; set; }

    public FromSinkTest? FromTest { get; set; }

    public override void Configure(LoggerConfiguration configurator)
    {
        Sink = new SinkTest();
        FromTest = new FromSinkTest();

        configurator.WriteTo.CategoryFilter(MessageCategory.Business | MessageCategory.Monitoring, Sink);
        configurator.WriteTo.Sink(FromTest);
    }
}
