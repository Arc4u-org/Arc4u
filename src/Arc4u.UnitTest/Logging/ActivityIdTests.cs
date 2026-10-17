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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    [Fact]
    public async Task ManageExceptionsFilter_Logs_The_Request_Context()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>());
        var context = CreateExceptionContext(new InvalidOperationException());
        context.HttpContext.Request.Method = HttpMethods.Post;
        context.HttpContext.Request.Path = "/orders/42";
        context.RouteData.Values["id"] = 42;

        await filter.OnExceptionAsync(context);

        sink.Properties.Should().ContainKeys("HttpMethod", "Path", "RouteValues");
        sink.Properties["Path"].ToString().Should().Contain("/orders/42");
    }

    [Fact]
    public async Task ManageExceptionsFilter_Hides_The_Exception_Details_By_Default()
    {
        var (serviceProvider, _) = BuildServices();
        var filter = new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>(),
                                                Options.Create(new ManageExceptionsFilterOptions()),
                                                new TestHostEnvironment(Environments.Production));
        var context = CreateExceptionContext(new InvalidOperationException("Secret"));

        await filter.OnExceptionAsync(context);

        var problem = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Extensions.Should().NotContainKey("exception");
    }

    [Theory]
    [InlineData(null, "Development")]
    [InlineData(true, "Production")]
    public async Task ManageExceptionsFilter_Returns_The_Exception_Details_When_Allowed(bool? includeExceptionDetails, string environmentName)
    {
        var (serviceProvider, _) = BuildServices();
        var filter = new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>(),
                                                Options.Create(new ManageExceptionsFilterOptions { IncludeExceptionDetails = includeExceptionDetails }),
                                                new TestHostEnvironment(environmentName));
        var context = CreateExceptionContext(new InvalidOperationException("Outer", new NullReferenceException("Inner")));

        await filter.OnExceptionAsync(context);

        var problem = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>().Which;
        var detail = problem.Extensions["exception"].Should().BeOfType<ManageExceptionsFilter.ExceptionDetail>().Which;
        detail.Type.Should().Be(typeof(InvalidOperationException).FullName);
        detail.Message.Should().Be("Outer");
        detail.InnerException!.Type.Should().Be(typeof(NullReferenceException).FullName);
        detail.InnerException.Message.Should().Be("Inner");
    }

    private static ManageExceptionsFilter CreateFilter(ServiceProvider serviceProvider, bool logActionArguments, int maxActionArgumentLength = 2048)
    {
        return new ManageExceptionsFilter(serviceProvider.GetRequiredService<ILogger<ManageExceptionsFilter>>(),
                                          Options.Create(new ManageExceptionsFilterOptions { LogActionArguments = logActionArguments, MaxActionArgumentLength = maxActionArgumentLength }),
                                          new TestHostEnvironment(Environments.Production));
    }

    private static async Task ExecuteFailingActionAsync(ManageExceptionsFilter filter, Dictionary<string, object?> arguments)
    {
        var exceptionContext = CreateExceptionContext(new InvalidOperationException());

        await filter.OnActionExecutionAsync(new ActionExecutingContext(exceptionContext, [], arguments, controller: new object()),
                                            () => Task.FromResult<ActionExecutedContext>(null!));
        await filter.OnExceptionAsync(exceptionContext);
    }

    [Fact]
    public async Task ManageExceptionsFilter_Logs_The_Action_Arguments_When_Enabled()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = CreateFilter(serviceProvider, logActionArguments: true);
        var arguments = new Dictionary<string, object?> { ["id"] = 42, ["order"] = new { Name = "Book" }, ["cancellationToken"] = CancellationToken.None, ["missing"] = null };

        await ExecuteFailingActionAsync(filter, arguments);

        sink.Properties.Should().ContainKey("ActionArguments");
        var logged = sink.Properties["ActionArguments"].ToString();
        logged.Should().Contain("42").And.Contain("Book").And.Contain("<CancellationToken>");
    }

    [Fact]
    public async Task ManageExceptionsFilter_Truncates_Long_Action_Arguments()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = CreateFilter(serviceProvider, logActionArguments: true, maxActionArgumentLength: 5);

        await ExecuteFailingActionAsync(filter, new Dictionary<string, object?> { ["text"] = new string('a', 100) });

        // The JSON of the string is "aaa...a": 5 characters are the opening quote and 4 letters.
        sink.Properties["ActionArguments"].ToString().Should().Contain("aaaa...").And.NotContain("aaaaa");
    }

    [Fact]
    public async Task ManageExceptionsFilter_Does_Not_Log_The_Action_Arguments_By_Default()
    {
        var (serviceProvider, sink) = BuildServices();
        var filter = CreateFilter(serviceProvider, logActionArguments: false);

        await ExecuteFailingActionAsync(filter, new Dictionary<string, object?> { ["id"] = 42 });

        sink.Properties.Should().NotContainKey("ActionArguments");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
