using System.Text;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class BasicAuthenticationMiddlewareTests
{
    [Fact]
    public async Task Basic_Header_Is_Replaced_And_The_Provider_Is_Called_On_Each_Request_Should()
    {
        // arrange
        var provider = new Mock<ITokenProvider>();
        provider.Setup(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.IsAny<object?>()))
                .ReturnsAsync(Result.Ok(new TokenInfo("Bearer", "access-token", DateTime.UtcNow.AddHours(1))));
        // A token cache is registered: the middleware must not use it, the provider owns the cache.
        var tokenCache = new Mock<ITokenCache>();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.AddApplicationConfig(config =>
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            config.ApplicationName = fixture.Create<string>();
            config.Environment = fixture.Create<Arc4u.Configuration.Environment>();
        });
        services.AddKeyedSingleton("Credential", provider.Object);
        services.AddSingleton(tokenCache.Object);
        var container = services.BuildServiceProvider();

        var options = new Mock<IOptionsMonitor<BasicAuthenticationSettingsOptions>>();
        options.Setup(o => o.CurrentValue).Returns(new BasicAuthenticationSettingsOptions
        {
            BasicSettings = new SimpleKeyValueSettings(new Dictionary<string, string> { [TokenKeys.ProviderIdKey] = "Credential" }),
            DefaultUpn = string.Empty,
            CertificateHeaderOptions = []
        });

        var authorizations = new List<string?>();
        var sut = new BasicAuthenticationMiddleware(
            ctx => { authorizations.Add(ctx.Request.Headers.Authorization); return Task.CompletedTask; },
            container.GetRequiredService<ILogger<BasicAuthenticationMiddleware>>(),
            options.Object,
            container);

        HttpContext NewRequest()
        {
            var context = new DefaultHttpContext { RequestServices = container };
            context.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user@arc4u.net:password"));
            return context;
        }

        // act
        await sut.Invoke(NewRequest());
        await sut.Invoke(NewRequest());

        // assert
        authorizations.Should().AllBe("Bearer access-token");
        provider.Verify(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.Is<object?>(o => o is CredentialsResult)), Times.Exactly(2));
        tokenCache.VerifyNoOtherCalls();
    }
}
