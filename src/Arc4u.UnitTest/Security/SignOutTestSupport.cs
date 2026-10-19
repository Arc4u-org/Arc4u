using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Security.Principal;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Environment = Arc4u.Configuration.Environment;

namespace Arc4u.UnitTest.Security;

/// <summary>The helpers shared by the sign-out tests (issue #113).</summary>
internal static class SignOutTestSupport
{
    internal static IApplicationContext PrincipalWithToken(string token)
    {
        var applicationContext = new Mock<IApplicationContext>();
        applicationContext.SetupGet(c => c.Principal).Returns(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), new ClaimsIdentity("Bearer") { BootstrapContext = token }, "S-1-0-0"));
        return applicationContext.Object;
    }

    // A container with an Arc4u logger, which the factories and the sign-out helper need to log.
    internal static ServiceProvider BuildContainer(Action<IServiceCollection> register)
    {
        var fixture = new Fixture();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.AddApplicationConfig(config =>
        {
            config.ApplicationName = fixture.Create<string>();
            config.Environment = fixture.Create<Environment>();
        });
        register(services);

        return services.BuildServiceProvider();
    }

    internal static string CreateJwt()
        => CreateJwt(DateTime.UtcNow.AddHours(1));

    internal static string CreateJwt(DateTime expires, params (string Type, string Value)[] claims)
        => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: "https://login.example.com", claims: claims.Select(c => new Claim(c.Type, c.Value)), expires: expires));

    internal static string CreateJwt(params (string Type, string Value)[] claims)
        => CreateJwt(DateTime.UtcNow.AddHours(1), claims);
}
