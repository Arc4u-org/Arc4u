using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore.Diagnostics;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class HttpContextIdentityTests
{
    // A singleton that holds a transient ILogger<T> for the lifetime of the application.
    private sealed class SingletonConsumer(ILogger<SingletonConsumer> logger)
    {
        public void Log() => logger.Technical().LogInformation("From a singleton");
    }

    private static (ServiceProvider, FromSinkTest) BuildServices(bool registerPropertiesFirst = false)
    {
        var sink = new FromSinkTest();

        var serilog = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .MinimumLevel.Debug()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog, false));
        if (registerPropertiesFirst)
        {
            services.AddHttpContextLoggingProperties();
            services.AddApplicationContext();
        }
        else
        {
            // The usual order in a host: AddApplicationContext, then the Arc4u authentication.
            services.AddApplicationContext();
            services.AddHttpContextLoggingProperties();
        }
        services.AddKeyedScoped<IAddPropertiesToLog, DefaultLoggingProperties>("Scoped");
        services.AddSingleton<SingletonConsumer>();

        return (services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }), sink);
    }

    // Simulates a request: its scope holds an IApplicationContext with an authenticated principal.
    private static DefaultHttpContext StartRequest(ServiceProvider serviceProvider, IServiceScope scope)
    {
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().SetPrincipal(IdentityTests.CreatePrincipal());

        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        return context;
    }

    private static object? Identity(FromSinkTest sink) =>
        sink.Properties.TryGetValue(LoggingConstants.Identity, out var value) ? ((ScalarValue)value).Value : null;

    [Theory]
    [InlineData(true)]
    public void AddHttpContextLoggingProperties_Replaces_The_Null_Transient_Provider_In_Any_Order(bool registerPropertiesFirst)
    {
        var (serviceProvider, _) = BuildServices(registerPropertiesFirst);

        serviceProvider.GetRequiredKeyedService<IAddPropertiesToLog>("Transient").Should().BeOfType<HttpContextLoggingProperties>();
    }

    [Fact]
    public void AddHttpContextLoggingProperties_Keeps_A_Custom_Transient_Provider()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IAddPropertiesToLog, TenantProperties>("Transient");
        services.AddApplicationContext();
        services.AddHttpContextLoggingProperties();

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredKeyedService<IAddPropertiesToLog>("Transient").Should().BeOfType<TenantProperties>();
    }

    [Fact]
    public void Logger_In_A_Singleton_Writes_Identity_Of_The_Current_Request()
    {
        var (serviceProvider, sink) = BuildServices(true);
        var consumer = serviceProvider.GetRequiredService<SingletonConsumer>();

        using (var scope = serviceProvider.CreateScope())
        {
            StartRequest(serviceProvider, scope);
            consumer.Log();
        }

        Identity(sink).Should().Be("alice@contoso.com");
    }

    [Fact]
    public void Logger_Writes_No_Identity_Outside_A_Request()
    {
        var (serviceProvider, sink) = BuildServices();
        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

        serviceProvider.GetRequiredService<ILogger<HttpContextIdentityTests>>().Technical().LogInformation("No request");

        sink.Properties.Should().NotContainKey(LoggingConstants.Identity);
    }

    [Fact]
    public void Logger_Writes_No_Identity_After_The_Request_Scope_Is_Disposed()
    {
        var (serviceProvider, sink) = BuildServices();
        var consumer = serviceProvider.GetRequiredService<SingletonConsumer>();

        var scope = serviceProvider.CreateScope();
        StartRequest(serviceProvider, scope);
        scope.Dispose();

        consumer.Log();

        sink.Properties.Should().NotContainKey(LoggingConstants.Identity);
    }

    private sealed class TenantProperties : IAddPropertiesToLog
    {
        public IDictionary<string, object> GetProperties() => new Dictionary<string, object> { ["Tenant"] = "contoso" };
    }
}
