using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore.Filters;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arc4u.UnitTest.Logging;

// MVC only discovers public top-level controllers.
[ApiController]
[Route("mvc")]
public sealed class ManageExceptionsTestController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(int id) => throw new InvalidOperationException($"MVC failure {id}");
}

[Trait("Category", "CI")]
public class ManageExceptionsHandlerTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }

    private static async Task<(WebApplication, CollectingSink)> StartServerAsync(string environmentName = "Production")
    {
        var sink = new CollectingSink();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(new LoggerConfiguration().WriteTo.Sink(sink).MinimumLevel.Debug().CreateLogger(), dispose: true);

        builder.Services.AddILogger();
        builder.Services.AddControllers(options => options.Filters.Add<ManageExceptionsFilter>())
                        .AddApplicationPart(typeof(ManageExceptionsTestController).Assembly);
        builder.Services.AddManageExceptionsHandler();

        var app = builder.Build();

        app.UseExceptionHandler();
        app.MapGet("/minimal/{id}", (int id) => { throw new InvalidOperationException($"Minimal failure {id}"); });
        app.MapGet("/forbidden", () => { throw new UnauthorizedAccessException(); });
        app.MapControllers();

        await app.StartAsync();

        return (app, sink);
    }

    private static List<LogEvent> ExceptionEntries(CollectingSink sink) => sink.Events.Where(e => e.Exception is not null).ToList();

    [Fact]
    public async Task Minimal_Api_Exception_Is_Logged_With_Its_Context_And_Returns_A_Generic_500()
    {
        var (app, sink) = await StartServerAsync();
        await using var _ = app;

        var response = await app.GetTestClient().GetAsync(new Uri("/minimal/42", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions.Should().NotContainKey("exception");

        // Only the handler logs it: the middleware does not log an exception an IExceptionHandler handled.
        var entry = ExceptionEntries(sink).Should().ContainSingle().Which;
        entry.Exception!.Message.Should().Be("Minimal failure 42");
        entry.Properties.Should().ContainKeys("HttpMethod", "Path", "Endpoint", "RouteValues");
        var activityId = ((ScalarValue)entry.Properties[LoggingConstants.ActivityId]).Value!.ToString()!;
        problem.Detail.Should().EndWith(activityId);
    }

    [Fact]
    public async Task Minimal_Api_UnauthorizedAccessException_Returns_403()
    {
        var (app, _) = await StartServerAsync();
        await using var __ = app;

        var response = await app.GetTestClient().GetAsync(new Uri("/forbidden", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Minimal_Api_Returns_The_Exception_Details_In_Development()
    {
        var (app, _) = await StartServerAsync(Environments.Development);
        await using var __ = app;

        var response = await app.GetTestClient().GetAsync(new Uri("/minimal/42", UriKind.Relative));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["exception"]!.ToString().Should().Contain("Minimal failure 42").And.Contain(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task Mvc_Exception_Is_Handled_By_The_Filter_Only()
    {
        var (app, sink) = await StartServerAsync();
        await using var __ = app;

        var response = await app.GetTestClient().GetAsync(new Uri("/mvc/7", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var entry = ExceptionEntries(sink).Should().ContainSingle().Which;
        ((ScalarValue)entry.Properties[LoggingConstants.Class]).Value.Should().Be(typeof(ManageExceptionsFilter).FullName);
        entry.Properties.Should().ContainKey("Action");
    }
}
