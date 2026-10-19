using System.Net;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Network.Connectivity;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static Arc4u.UnitTest.Security.SignOutTestSupport;

namespace Arc4u.UnitTest.Security;

/// <summary>The sign-out of the client token providers: <see cref="ClientTokenProvider"/>, <see cref="BlazorMsalTokenProvider"/> and <see cref="UsernamePasswordTokenProvider"/> (issue #113).</summary>
[Trait("Category", "CI")]
public class ClientTokenProvidersSignOutTests
{
    [Fact]
    public async Task Client_SignOut_Should_Ask_The_Backend_Again()
    {
        var handler = new TokenHandler(CreateJwt());
        var (sut, settings) = CreateClientTokenProvider(handler);

        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();
        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();
        handler.Calls.Should().Be(1);

        await sut.SignOutAsync(settings, CancellationToken.None);
        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Client_Token_Requested_Before_SignOut_Should_Not_Be_Kept()
    {
        var handler = new TokenHandler(CreateJwt()) { Gate = new TaskCompletionSource() };
        var (sut, settings) = CreateClientTokenProvider(handler);

        var inFlight = sut.GetTokenAsync(settings, null).AsTask();
        await sut.SignOutAsync(settings, CancellationToken.None);
        handler.Gate.SetResult();

        (await inFlight).IsFailed.Should().BeTrue();

        handler.Gate = null;
        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();
        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Client_Token_Expiring_In_Less_Than_One_Minute_Should_Be_Renewed()
    {
        var handler = new TokenHandler(CreateJwt(DateTime.UtcNow.AddSeconds(30)));
        var (sut, settings) = CreateClientTokenProvider(handler);

        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();
        (await sut.GetTokenAsync(settings, null)).IsSuccess.Should().BeTrue();

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task BlazorMsal_SignOut_Should_Clear_The_Principal()
    {
        var applicationContext = new Mock<IApplicationContext>();
        var sut = new BlazorMsalTokenProvider(new Mock<IAccessTokenProvider>().Object, applicationContext.Object, new Mock<NavigationManager>().Object);

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        applicationContext.Verify(c => c.SetPrincipal(null), Times.Once);
    }

    [Fact]
    public async Task UsernamePassword_SignOut_With_The_Settings_Of_Another_Provider_Should_Do_Nothing()
    {
        var secureCache = new Mock<ISecureCache>();
        var sut = new UsernamePasswordTokenProvider(secureCache.Object, new Mock<INetworkInformation>().Object, NullLogger<UsernamePasswordTokenProvider>.Instance, new Mock<IServiceProvider>().Object);

        await sut.SignOutAsync(new SimpleKeyValueSettings(new Dictionary<string, string> { { "ProviderId", "Oidc" } }), CancellationToken.None);

        secureCache.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UsernamePassword_SignOut_Should_Remove_The_User_Name_The_Password_And_The_Token()
    {
        var secureCache = new Mock<ISecureCache>();
        var sut = new UsernamePasswordTokenProvider(secureCache.Object, new Mock<INetworkInformation>().Object, NullLogger<UsernamePasswordTokenProvider>.Instance, new Mock<IServiceProvider>().Object);
        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.AuthorityKey, "Default" },
            { TokenKeys.ServiceApplicationIdKey, "service" }
        });

        await sut.SignOutAsync(settings, CancellationToken.None);

        secureCache.Verify(c => c.Remove("secret_upn"), Times.Once);
        secureCache.Verify(c => c.Remove("secret_pwd"), Times.Once);
        secureCache.Verify(c => c.Remove("service"), Times.Once);
    }

    private static (ClientTokenProvider Provider, SimpleKeyValueSettings Settings) CreateClientTokenProvider(TokenHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("backend")).Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://backend.example.com") });
        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.HttpClientName, "backend" },
            { TokenKeys.TokenRequestUrl, "/token" }
        });

        return (new ClientTokenProvider(factory.Object, NullLogger<ClientTokenProvider>.Instance), settings);
    }

    private sealed class TokenHandler(string token) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        // When set, the response waits for it: the request is in flight until the test completes it.
        public TaskCompletionSource? Gate { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Gate is not null)
            {
                await Gate.Task.ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"\"{token}\"", Encoding.UTF8, "application/json")
            };
        }
    }
}
