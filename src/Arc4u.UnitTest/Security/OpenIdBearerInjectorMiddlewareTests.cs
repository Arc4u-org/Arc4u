using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The middleware puts the token it obtains (on-behalf-of or not) in the <c>Authorization</c> header and in the <see cref="ClaimsIdentity.BootstrapContext"/> of the identity (issue #158).
/// </summary>
[Trait("Category", "CI")]
public class OpenIdBearerInjectorMiddlewareTests
{
    private const string OidcProviderKey = "Oidc";
    private const string OboProviderKey = "Obo";

    [Fact]
    public async Task InvokeAsync_Should_Set_The_Token_In_The_Header_And_The_BootstrapContext()
    {
        var oidcProvider = ProviderReturning(Result.Ok(new TokenInfo("access_token", "oidc-token", DateTime.UtcNow.AddHours(1))));
        var (sut, context, nextCalled) = Create(Constants.CookiesAuthenticationType, oidcProvider, oboProvider: null, withOboSettings: false);

        await sut.InvokeAsync(context, new DefaultActivitySourceFactory(), NullLogger<OpenIdBearerInjectorMiddleware>.Instance);

        context.Request.Headers.Authorization.ToString().Should().Be("Bearer oidc-token");
        ((ClaimsIdentity)context.User.Identity!).BootstrapContext.Should().Be("oidc-token");
        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_With_OnBehalfOf_Settings_Should_Set_The_OnBehalfOf_Token_In_The_Header_And_The_BootstrapContext()
    {
        var oidcProvider = new Mock<ITokenProvider>();
        var oboProvider = ProviderReturning(Result.Ok(new TokenInfo("access_token", "obo-token", DateTime.UtcNow.AddHours(1))));
        var (sut, context, nextCalled) = Create(Constants.CookiesAuthenticationType, oidcProvider, oboProvider, withOboSettings: true);

        await sut.InvokeAsync(context, new DefaultActivitySourceFactory(), NullLogger<OpenIdBearerInjectorMiddleware>.Instance);

        context.Request.Headers.Authorization.ToString().Should().Be("Bearer obo-token");
        ((ClaimsIdentity)context.User.Identity!).BootstrapContext.Should().Be("obo-token");
        oidcProvider.Verify(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.IsAny<object?>()), Times.Never);
        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_When_The_Token_Cannot_Be_Obtained_Should_Leave_The_Header_And_The_BootstrapContext_Unchanged()
    {
        var oidcProvider = ProviderReturning(Result.Fail<TokenInfo>("no token"));
        var (sut, context, nextCalled) = Create(Constants.CookiesAuthenticationType, oidcProvider, oboProvider: null, withOboSettings: false);
        context.Request.Headers.Authorization = "Bearer previous";
        ((ClaimsIdentity)context.User.Identity!).BootstrapContext = "previous";

        await sut.InvokeAsync(context, new DefaultActivitySourceFactory(), NullLogger<OpenIdBearerInjectorMiddleware>.Instance);

        context.Request.Headers.Authorization.ToString().Should().Be("Bearer previous");
        ((ClaimsIdentity)context.User.Identity!).BootstrapContext.Should().Be("previous");
        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_With_Another_AuthenticationType_Should_Not_Request_A_Token()
    {
        var oidcProvider = ProviderReturning(Result.Ok(new TokenInfo("access_token", "oidc-token", DateTime.UtcNow.AddHours(1))));
        var (sut, context, nextCalled) = Create(Constants.BearerAuthenticationType, oidcProvider, oboProvider: null, withOboSettings: false);

        await sut.InvokeAsync(context, new DefaultActivitySourceFactory(), NullLogger<OpenIdBearerInjectorMiddleware>.Instance);

        context.Request.Headers.ContainsKey("Authorization").Should().BeFalse();
        ((ClaimsIdentity)context.User.Identity!).BootstrapContext.Should().BeNull();
        oidcProvider.Verify(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.IsAny<object?>()), Times.Never);
        nextCalled().Should().BeTrue();
    }

    private static Mock<ITokenProvider> ProviderReturning(Result<TokenInfo> result)
    {
        var provider = new Mock<ITokenProvider>();
        provider.Setup(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.IsAny<object?>())).ReturnsAsync(result);
        return provider;
    }

    private static (OpenIdBearerInjectorMiddleware Middleware, DefaultHttpContext Context, Func<bool> NextCalled) Create(string authenticationType, Mock<ITokenProvider> oidcProvider, Mock<ITokenProvider>? oboProvider, bool withOboSettings)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton(OidcProviderKey, oidcProvider.Object);
        if (oboProvider is not null)
        {
            services.AddKeyedSingleton(OboProviderKey, oboProvider.Object);
        }

        var settings = new OpenIdBearerInjectorSettingsOptions
        {
            OboProviderKey = OboProviderKey,
            OpenIdSettings = new SimpleKeyValueSettings(new Dictionary<string, string>
            {
                [TokenKeys.AuthenticationTypeKey] = Constants.CookiesAuthenticationType,
                [TokenKeys.ProviderIdKey] = OidcProviderKey,
            }),
            OnBehalfOfOpenIdSettings = withOboSettings
                ? new SimpleKeyValueSettings(new Dictionary<string, string> { [TokenKeys.ProviderIdKey] = OboProviderKey })
                : new SimpleKeyValueSettings(),
        };
        var options = new Mock<IOptionsMonitor<OpenIdBearerInjectorSettingsOptions>>();
        options.SetupGet(o => o.CurrentValue).Returns(settings);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "John")], authenticationType)),
        };

        var nextCalled = false;
        var sut = new OpenIdBearerInjectorMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options.Object);

        return (sut, context, () => nextCalled);
    }
}
