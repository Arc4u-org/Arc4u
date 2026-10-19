using System.Collections.Concurrent;
using System.Text;
using Arc4u.Diagnostics;
using Arc4u.AspNetCore.Middleware;
using Arc4u.OAuth2.AspNetCore.Diagnostics;
using Arc4u.Security.Principal;
using Arc4u.UnitTest.Logging;
using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arc4u.UnitTest.gRPC;

/// <summary>
/// A gRPC call hosted by ASP.NET Core is an HTTP/2 request: the loggers resolved in its scope use the "Scoped" provider,
/// and the logger of a singleton gets the identity of the call through <see cref="HttpContextLoggingProperties"/>.
/// </summary>
[Trait("Category", "CI")]
public class GrpcIdentityLoggingTests
{
    private const string FromService = "From the gRPC service";
    private const string FromSingleton = "From a singleton";
    private const string FromSingletonInTaskRun = "In Task.Run, from a singleton";

    private sealed class CollectingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);

        public object? Identity(string messageStart) =>
            Events.Single(e => e.MessageTemplate.Text.StartsWith(messageStart, StringComparison.Ordinal))
                  .Properties.TryGetValue(LoggingConstants.Identity, out var value) ? ((ScalarValue)value).Value : null;
    }

    public sealed class SingletonConsumer(ILogger<SingletonConsumer> logger)
    {
        public void Log(string message) => logger.Technical().LogInformation(message);
    }

    // A code-first gRPC service shaped like the generated code: one unary method exchanging strings, declared virtual on a base type
    // that binds it (Grpc.AspNetCore only binds the overrides of the base type's methods).
    public static class IdentityGrpc
    {
        private static readonly Marshaller<string> StringMarshaller = Marshallers.Create(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);

        public static readonly Method<string, string> LogMethod = new(MethodType.Unary, "test.Identity", "Log", StringMarshaller, StringMarshaller);

        [BindServiceMethod(typeof(IdentityGrpc), nameof(BindService))]
        public abstract class IdentityGrpcBase
        {
            public virtual Task<string> Log(string request, ServerCallContext context) => throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public static void BindService(ServiceBinderBase binder, IdentityGrpcBase? service) =>
            binder.AddMethod(LogMethod, service is null ? null! : new UnaryServerMethod<string, string>(service.Log));
    }

    public sealed class IdentityGrpcService(ILogger<IdentityGrpcService> logger, SingletonConsumer singleton) : IdentityGrpc.IdentityGrpcBase
    {
        public override async Task<string> Log(string request, ServerCallContext context)
        {
            logger.Technical().LogInformation(FromService);
            singleton.Log(FromSingleton);
            await Task.Run(() => singleton.Log(FromSingletonInTaskRun)).ConfigureAwait(false);
            return request;
        }
    }

    private static async Task<(WebApplication, CollectingSink)> StartServerAsync()
    {
        var sink = new CollectingSink();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(new LoggerConfiguration().WriteTo.Sink(sink).MinimumLevel.Debug().CreateLogger(), dispose: true);

        builder.Services.AddGrpc();
        // Before AddApplicationContext, which registers a NullLoggerProperties under "Transient".
        builder.Services.AddHttpContextLoggingProperties();
        builder.Services.AddApplicationContext();
        builder.Services.AddKeyedScoped<IAddPropertiesToLog, DefaultLoggingProperties>("Scoped");
        builder.Services.AddSingleton<SingletonConsumer>();

        var app = builder.Build();

        app.AddGrpcMonitoringTimeElapsed();
        // Stands in for the Arc4u authentication: AppPrincipalTransform sets the principal on the scoped IApplicationContext of the request.
        app.Use((context, next) =>
        {
            context.RequestServices.GetRequiredService<IApplicationContext>().SetPrincipal(IdentityTests.CreatePrincipal());
            return next(context);
        });
        app.MapGrpcService<IdentityGrpcService>();

        await app.StartAsync();

        return (app, sink);
    }

    [Fact]
    public async Task Loggers_Write_Identity_Of_The_gRPC_Call()
    {
        var (app, sink) = await StartServerAsync();
        await using var _ = app;

        var server = app.GetTestServer();
        using var channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions { HttpHandler = server.CreateHandler() });

        var response = await channel.CreateCallInvoker().AsyncUnaryCall(IdentityGrpc.LogMethod, null, new CallOptions(), "ping");

        response.Should().Be("ping");
        sink.Identity(FromService).Should().Be("alice@contoso.com");
        sink.Identity(FromSingleton).Should().Be("alice@contoso.com");
        sink.Identity(FromSingletonInTaskRun).Should().Be("alice@contoso.com");
        sink.Identity("Time to complete").Should().Be("alice@contoso.com");
    }

    [Fact]
    public async Task Singleton_Logger_Writes_No_Identity_Outside_A_gRPC_Call()
    {
        var (app, sink) = await StartServerAsync();
        await using var _ = app;

        app.Services.GetRequiredService<SingletonConsumer>().Log(FromSingleton);

        sink.Identity(FromSingleton).Should().BeNull();
    }
}
