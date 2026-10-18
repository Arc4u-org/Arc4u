using System.Security.Claims;
using Arc4u.OAuth2.Middleware;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ValidateSwaggerRightMiddlewareTests
{
    private const int Access = 1;

    private static readonly ValidateSwaggerRightMiddlewareOption Option = new() { Access = Access, Path = "/swagger/v1/swagger.json" };

    private static AppPrincipal CreatePrincipal(params int[] operations)
    {
        var authorization = new Arc4u.Security.Principal.Authorization
        {
            Operations = [new ScopedOperations { Scope = "", Operations = [.. operations] }],
            AllOperations = [new Operation { ID = Access, Name = "AccessSwagger" }],
            Scopes = [""],
        };
        return new AppPrincipal(authorization, new ClaimsIdentity("TestAuth"), "S-1-0-0");
    }

    private static DefaultHttpContext BuildContext(string path, AppPrincipal principal)
    {
        var applicationContext = new Mock<IApplicationContext>();
        applicationContext.Setup(x => x.Principal).Returns(principal);

        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(applicationContext.Object).BuildServiceProvider(),
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<(bool NextCalled, string Body)> InvokeAsync(string path, AppPrincipal principal)
    {
        var nextCalled = false;
        var sut = new ValidateSwaggerRightMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, Option);
        var context = BuildContext(path, principal);

        await sut.Invoke(context);

        context.Response.Body.Position = 0;
        return (nextCalled, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    // Issue #112: Swashbuckle serves the document for another extension, so changing the extension bypassed the check.
    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/SWAGGER/v1/swagger.json/")]
    [InlineData("/swagger/v1/swagger.yaml")]
    [InlineData("/swagger/v1/swagger.yml")]
    [InlineData("/swagger/v1/swagger.jsonx")]
    [InlineData("/swagger/v1/swagger.json.x")]
    public async Task Invoke_Without_Right_Writes_The_Content_To_Display(string path)
    {
        var (nextCalled, body) = await InvokeAsync(path, CreatePrincipal());

        nextCalled.Should().BeFalse();
        body.Should().Be(Option.ContentToDisplay);
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/v1/swagger.yaml")]
    public async Task Invoke_With_Right_Calls_Next(string path)
    {
        var (nextCalled, body) = await InvokeAsync(path, CreatePrincipal(Access));

        nextCalled.Should().BeTrue();
        body.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v2/swagger.json")]
    [InlineData("/swagger/v1/swagger")]
    [InlineData("/swagger/v1/swaggerx.json")]
    [InlineData("/swagger/v1/swagger.json/x")]
    public async Task Invoke_On_Another_Path_Calls_Next(string path)
    {
        var (nextCalled, _) = await InvokeAsync(path, CreatePrincipal());

        nextCalled.Should().BeTrue();
    }
}
