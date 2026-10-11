using System.Security.Claims;
using Arc4u.Diagnostics;
using Arc4u.Dependency;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Arc4u.UnitTest.Logging;

[Trait("Category", "CI")]
public class IdentityTests
{
    // A singleton that holds a transient ILogger<T> for the lifetime of the application.
    private sealed class SingletonConsumer(ILogger<SingletonConsumer> logger)
    {
        public void Log() => logger.Technical().LogInformation("From a singleton");
    }

    private sealed class TenantProperties : IAddPropertiesToLog
    {
        public IDictionary<string, object> GetProperties() => new Dictionary<string, object> { ["Tenant"] = "contoso" };
    }

    private static (ServiceProvider, FromSinkTest) BuildServices()
    {
        var sink = new FromSinkTest();

        var serilog = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .MinimumLevel.Debug()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog, false));
        services.AddKeyedTransient<IAddPropertiesToLog, TenantProperties>("Transient");
        services.AddApplicationContext();
        services.AddKeyedScoped<IAddPropertiesToLog, DefaultLoggingProperties>("Scoped");
        services.AddSingleton<SingletonConsumer>();

        return (services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }), sink);
    }

    // Identity is the email of the profile, or the identity name ("alice") when the profile has no email.
    internal static AppPrincipal CreatePrincipal(string email = "alice@contoso.com") =>
        new AppPrincipal(new global::Arc4u.Security.Principal.Authorization(), new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "TestAuth"), "S-1-0-0")
        {
            Profile = new UserProfile(displayName: "Alice", email: email, department: "", company: "", givenName: "", surName: "", sid: "S-1-0-0", state: "",
                                      mobile: "", telephone: "", internalPhone: "", fax: "", principalName: "", postalCode: "", street: "", room: "",
                                      initials: "", samAccountName: "alice", domain: "contoso", culture: System.Globalization.CultureInfo.InvariantCulture,
                                      commonName: "", description: "")
        };

    private static object? Identity(FromSinkTest sink) =>
        sink.Properties.TryGetValue(LoggingConstants.Identity, out var value) ? ((ScalarValue)value).Value : null;

    [Fact]
    public void Logger_Resolved_In_A_Scope_Writes_Identity_Of_The_ApplicationContext()
    {
        var (serviceProvider, sink) = BuildServices();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().SetPrincipal(CreatePrincipal());

        scope.ServiceProvider.GetRequiredService<ILogger<IdentityTests>>().Technical().LogInformation("In a scope");

        Identity(sink).Should().Be("alice@contoso.com");
        sink.Properties.Should().NotContainKey("Tenant");
    }

    [Fact]
    public void Logger_Writes_The_Identity_Name_When_The_Profile_Has_No_Email()
    {
        var (serviceProvider, sink) = BuildServices();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().SetPrincipal(CreatePrincipal(email: ""));

        scope.ServiceProvider.GetRequiredService<ILogger<IdentityTests>>().Technical().LogInformation("Without email");

        Identity(sink).Should().Be("alice");
    }

    [Fact]
    public void NonGeneric_Logger_Resolved_In_A_Scope_Writes_Identity_Of_The_ApplicationContext()
    {
        var (serviceProvider, sink) = BuildServices();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().SetPrincipal(CreatePrincipal());

        scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger>().Technical<IdentityTests>().LogInformation("In a scope");

        Identity(sink).Should().Be("alice@contoso.com");
    }

    [Fact]
    public void Logger_Resolved_In_A_Scope_Writes_No_Identity_Without_Principal()
    {
        var (serviceProvider, sink) = BuildServices();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ILogger<IdentityTests>>().Technical().LogInformation("Without identity");

        sink.Properties.Should().NotContainKey(LoggingConstants.Identity);
    }

    [Fact]
    public void Logger_Resolved_From_The_Root_Uses_The_Transient_Provider()
    {
        var (serviceProvider, sink) = BuildServices();

        serviceProvider.GetRequiredService<ILogger<IdentityTests>>().Technical().LogInformation("From the root");

        sink.Properties.Should().ContainKey("Tenant");
        sink.Properties.Should().NotContainKey(LoggingConstants.Identity);
    }

    [Fact]
    public void Logger_Of_A_Singleton_Resolved_In_A_Scope_Uses_The_Transient_Provider()
    {
        var (serviceProvider, sink) = BuildServices();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().SetPrincipal(CreatePrincipal());

        // The singleton is created from the root even when it is first requested in a scope: its logger must not capture the scope.
        scope.ServiceProvider.GetRequiredService<SingletonConsumer>().Log();

        sink.Properties.Should().ContainKey("Tenant");
        sink.Properties.Should().NotContainKey(LoggingConstants.Identity);
    }
}
