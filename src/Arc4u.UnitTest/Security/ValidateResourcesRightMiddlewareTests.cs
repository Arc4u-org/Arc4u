using System.Security.Claims;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ValidateResourcesRightMiddlewareTests
{
    private const string ContentToDisplay = "You are not authorized!";

    private static readonly ValidateResourcesRightMiddlewareOptions Options = new()
    {
        ResourcesPolicies = new()
        {
            ["OpenApi"] = new ValidateResourceRightMiddlewareOptions
            {
                Path = "/swagger/v1/swagger.json",
                AuthorizationPolicy = "Read",
                ContentToDisplay = ContentToDisplay,
            },
        },
    };

    private static AppPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new AppPrincipal(new Arc4u.Security.Principal.Authorization(), new ClaimsIdentity(claims, "TestAuth"), "S-1-0-0");
    }

    private static async Task<(bool NextCalled, string Body)> InvokeAsync(string path, AppPrincipal principal)
    {
        var applicationContext = new Mock<IApplicationContext>();
        applicationContext.Setup(x => x.Principal).Returns(principal);

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(applicationContext.Object);
        services.AddAuthorization(options => options.AddPolicy("Read", policy => policy.RequireClaim("read")));

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        var sut = new ValidateResourcesRightMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, Options);

        await sut.InvokeAsync(context);

        context.Response.Body.Position = 0;
        return (nextCalled, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    // Issue #112: Swashbuckle serves the document for another extension, so changing the extension bypassed the policy.
    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/SWAGGER/v1/swagger.json/")]
    [InlineData("/swagger/v1/swagger.yaml")]
    [InlineData("/swagger/v1/swagger.yml")]
    [InlineData("/swagger/v1/swagger.jsonx")]
    [InlineData("/swagger/v1/swagger.json.x")]
    public async Task InvokeAsync_Without_Policy_Writes_The_Content_To_Display(string path)
    {
        var (nextCalled, body) = await InvokeAsync(path, CreatePrincipal());

        nextCalled.Should().BeFalse();
        body.Should().Be(ContentToDisplay);
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/v1/swagger.yaml")]
    public async Task InvokeAsync_With_Policy_Calls_Next(string path)
    {
        var (nextCalled, body) = await InvokeAsync(path, CreatePrincipal(new Claim("read", "true")));

        nextCalled.Should().BeTrue();
        body.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v2/swagger.json")]
    [InlineData("/swagger/v1/swagger")]
    [InlineData("/swagger/v1/swaggerx.json")]
    [InlineData("/swagger/v1/swagger.json/x")]
    public async Task InvokeAsync_On_Another_Path_Calls_Next(string path)
    {
        var (nextCalled, _) = await InvokeAsync(path, CreatePrincipal());

        nextCalled.Should().BeTrue();
    }
}
