using System.Diagnostics;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore.Filters;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class ActivityIdTests
{
    private static (ServiceProvider, FromSinkTest) BuildServices()
    {
        var sink = new FromSinkTest();

        var serilog = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .MinimumLevel.Debug()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog, false));
        services.AddILogger();

        return (services.BuildServiceProvider(), sink);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, []) { Exception = exception };
    }

    [Fact]
    public void Logger_Writes_TraceId_Of_Current_Activity_As_ActivityId()
    {
        var (serviceProvider, sink) = BuildServices();
        var logger = serviceProvider.GetRequiredService<ILogger<ActivityIdTests>>();

        using var activity = new Activity("test").Start();

        logger.Technical().LogInformation("With activity");

        sink.ActivityId.Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public void Logger_Writes_No_ActivityId_Without_Current_Activity()
    {
        var (serviceProvider, sink) = BuildServices();
        var logger = serviceProvider.GetRequiredService<ILogger<ActivityIdTests>>();

        Activity.Current = null;

        logger.Technical().LogInformation("Without activity");

        sink.Properties.Should().NotContainKey(LoggingConstants.ActivityId);
    }

    [Fact]
    public async Task ManageExceptionsFilter_Returns_403_For_UnauthorizedAccessException()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>());
        var context = CreateExceptionContext(new UnauthorizedAccessException());

        using var activity = new Activity("request").Start();

        await filter.OnExceptionAsync(context);

        var problem = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status403Forbidden);
        sink.ActivityId.Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public async Task ManageExceptionsFilter_Returns_500_With_The_Logged_ActivityId_When_No_Activity_Exists()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>());
        var context = CreateExceptionContext(new InvalidOperationException());

        Activity.Current = null;

        await filter.OnExceptionAsync(context);

        var problem = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        sink.ActivityId.Should().NotBeNullOrEmpty();
        problem.Detail.Should().EndWith(sink.ActivityId);
        Activity.Current.Should().BeNull();
    }
}
