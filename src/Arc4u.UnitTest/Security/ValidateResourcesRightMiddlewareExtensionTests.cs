using Arc4u.Configuration;
using Arc4u.OAuth2.Middleware;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ValidateResourcesRightMiddlewareExtensionTests
{
    private static ApplicationBuilder BuildApp(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        return new ApplicationBuilder(services.BuildServiceProvider());
    }

    [Fact]
    public void UseResourcesRightValidationFor_Throws_When_Section_Does_Not_Exist()
    {
        var app = BuildApp(new Dictionary<string, string?>
        {
            ["Authentication:ResourcesRights:ResourcesPolicies:OpenApi:Path"] = "/swagger/v1/swagger.json",
            ["Authentication:ResourcesRights:ResourcesPolicies:OpenApi:AuthorizationPolicy"] = "Read",
        });

        var act = () => app.UseResourcesRightValidationFor("Authentication:ResourceRights");

        act.Should().Throw<ConfigurationException>().WithMessage("*Authentication:ResourceRights*");
    }

    [Fact]
    public void UseResourcesRightValidationFor_Does_Not_Throw_When_Section_Exists()
    {
        var app = BuildApp(new Dictionary<string, string?>
        {
            ["Authentication:ResourcesRights:ResourcesPolicies:OpenApi:Path"] = "/swagger/v1/swagger.json",
            ["Authentication:ResourcesRights:ResourcesPolicies:OpenApi:AuthorizationPolicy"] = "Read",
        });

        var act = () => app.UseResourcesRightValidationFor();

        act.Should().NotThrow();
    }
}
