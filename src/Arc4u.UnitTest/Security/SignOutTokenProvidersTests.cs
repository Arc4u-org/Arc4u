using System.Security.Claims;
using Arc4u.Blazor.Options;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.Network.Connectivity;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Client.Authentication.TokenProvider;
using Arc4u.OAuth2.Client.Security.Principal;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.OAuth2.TokenProviders;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using FluentResults;
using Result = FluentResults.Result;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static Arc4u.UnitTest.Security.SignOutTestSupport;

namespace Arc4u.UnitTest.Security;

/// <summary>Which providers sign out, <see cref="SignOutTokenProviderExtensions.SignOutTokenProvidersAsync"/>, <see cref="SignOutTokenProviderKeys"/> and the sign-out of the principal factories (issue #113).</summary>
[Trait("Category", "CI")]
public class SignOutTokenProvidersTests
{
    [Theory]
    [InlineData(typeof(RemoteClientSecretTokenProvider))]
    [InlineData(typeof(ClientCredentialsTokenProvider))]
    [InlineData(typeof(BootstrapContextTokenProvider))]
    [InlineData(typeof(NullTokenProvider))]
    [InlineData(typeof(OidcTokenProvider))]
    [InlineData(typeof(CredentialTokenCacheTokenProvider))]
    public void Providers_Without_User_Session_Should_Not_Sign_Out(Type providerType)
    {
        typeof(ITokenProvider).IsAssignableFrom(providerType).Should().BeTrue();
        typeof(ISignOutTokenProvider).IsAssignableFrom(providerType).Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(AzureADOboTokenProvider))]
    [InlineData(typeof(ClientTokenProvider))]
    [InlineData(typeof(BlazorMsalTokenProvider))]
    [InlineData(typeof(BlazorTokenProvider))]
    [InlineData(typeof(UsernamePasswordTokenProvider))]
    [InlineData(typeof(OidcClientIdentityModelTokenProvider))]
    public void Providers_With_User_Session_Should_Sign_Out(Type providerType)
    {
        typeof(ISignOutTokenProvider).IsAssignableFrom(providerType).Should().BeTrue();
    }

    [Fact]
    public void SignOutTokenProviderKeys_Should_Retry_A_Failed_Computation()
    {
        var services = new FlakyServiceCollection();
        services.AddKeyedSingleton<ITokenProvider, SingletonSignOutProvider>("singleton");
        var sut = new SignOutTokenProviderKeys(services);

        var first = Record.Exception(() => sut.Keys);

        first.Should().BeOfType<InvalidOperationException>();
        sut.Keys.Should().BeEquivalentTo(["singleton"]);
    }

    [Fact]
    public async Task Server_SignOutUser_With_A_Plain_Logger_Should_Sign_Out_The_Providers_When_The_Claims_Cache_Fails()
    {
        var provider = new Mock<ISignOutTokenProvider>();
        var claimsCache = new Mock<ICache>();
        claimsCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("cache down"));
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(claimsCache.Object);
        var keyGenerator = new Mock<ICacheKeyGenerator>();
        keyGenerator.Setup(g => g.GetClaimsKey(It.IsAny<ClaimsIdentity>())).Returns("user_ClaimsCache");
        var container = BuildContainer(services =>
        {
            services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object);
            services.AddSingleton(PrincipalWithToken("token"));
            services.AddSingleton(cacheHelper.Object);
            services.AddSingleton(keyGenerator.Object);
        });

        // A plain Microsoft.Extensions.Logging logger: Technical() throws for it.
        var sut = new AppServicePrincipalFactory(container,
                                                 NullLogger<AppServicePrincipalFactory>.Instance,
                                                 new Mock<IOptionsMonitor<SimpleKeyValueSettings>>().Object,
                                                 new Mock<IClaimsTransformation>().Object,
                                                 new Mock<IActivitySourceFactory>().Object);

        var exception = await Record.ExceptionAsync(async () => await sut.SignOutUserAsync(new SimpleKeyValueSettings(), CancellationToken.None));

        exception.Should().BeNull();
        provider.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutTokenProviders_Should_Sign_Out_Every_Provider_Once()
    {
        var shared = new Mock<ISignOutTokenProvider>();
        var other = new Mock<ISignOutTokenProvider>();
        var container = BuildContainer(services =>
        {
            services.AddKeyedSingleton("a", shared.As<ITokenProvider>().Object);
            services.AddKeyedSingleton("b", shared.As<ITokenProvider>().Object);
            services.AddKeyedSingleton("c", other.As<ITokenProvider>().Object);
            services.AddKeyedSingleton("d", new Mock<ITokenProvider>(MockBehavior.Strict).Object);
        });
        var settings = new SimpleKeyValueSettings();

        await container.SignOutTokenProvidersAsync(settings, container.GetRequiredService<ILogger<SignOutTokenProvidersTests>>(), CancellationToken.None);

        shared.Verify(p => p.SignOutAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
        other.Verify(p => p.SignOutAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutTokenProviders_Should_Continue_When_A_Provider_Throws()
    {
        var failing = new Mock<ISignOutTokenProvider>();
        failing.Setup(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var other = new Mock<ISignOutTokenProvider>();
        var container = BuildContainer(services =>
        {
            services.AddKeyedSingleton("a", failing.As<ITokenProvider>().Object);
            services.AddKeyedSingleton("b", other.As<ITokenProvider>().Object);
        });

        var exception = await Record.ExceptionAsync(async () => await container.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), container.GetRequiredService<ILogger<SignOutTokenProvidersTests>>(), CancellationToken.None));

        exception.Should().BeNull();
        other.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutTokenProviders_Should_Continue_When_The_Logger_Is_Not_An_Arc4u_Logger()
    {
        var failing = new Mock<ISignOutTokenProvider>();
        failing.Setup(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var other = new Mock<ISignOutTokenProvider>();
        var container = BuildContainer(services =>
        {
            services.AddKeyedSingleton("a", failing.As<ITokenProvider>().Object);
            services.AddKeyedSingleton("b", other.As<ITokenProvider>().Object);
        });

        var exception = await Record.ExceptionAsync(async () => await container.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), NullLogger<SignOutTokenProvidersTests>.Instance, CancellationToken.None));

        exception.Should().BeNull();
        other.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutTokenProviders_Without_Keys_Should_Not_Sign_Out_When_A_Provider_Cannot_Be_Resolved()
    {
        // Without AddTokenProvidersSignOut, all the keyed providers are resolved together: from the root provider the scoped one cannot be,
        // so no provider is signed out (the failure is logged). This is why the setup methods register the keys.
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITokenProvider, SingletonSignOutProvider>("singleton");
        services.AddKeyedScoped<ITokenProvider, ScopedSignOutProvider>("scoped");
        using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await root.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), NullLogger<SignOutTokenProvidersTests>.Instance, CancellationToken.None);

        root.GetRequiredKeyedService<ITokenProvider>("singleton").Should().BeOfType<SingletonSignOutProvider>().Which.SignOuts.Should().Be(0);
    }

    [Fact]
    public void AddAuthenticationCookie_Should_Register_The_Sign_Out_Keys()
    {
        var services = new ServiceCollection();

        services.AddAuthenticationCookie(_ => { });

        services.Should().ContainSingle(d => d.ServiceType == typeof(SignOutTokenProviderKeys));
    }

    [Fact]
    public async Task SignOutTokenProviders_With_Keys_Should_Sign_Out_The_Providers_That_Can_Be_Resolved()
    {
        // From the root provider, the scoped provider cannot be resolved: the singleton one is signed out anyway.
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITokenProvider, SingletonSignOutProvider>("singleton");
        services.AddKeyedScoped<ITokenProvider, ScopedSignOutProvider>("scoped");
        services.AddTokenProvidersSignOut();
        using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await root.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), NullLogger<SignOutTokenProvidersTests>.Instance, CancellationToken.None);

        root.GetRequiredKeyedService<ITokenProvider>("singleton").Should().BeOfType<SingletonSignOutProvider>().Which.SignOuts.Should().Be(1);
    }

    [Fact]
    public async Task SignOutTokenProviders_With_Keys_Should_Sign_Out_The_Scoped_Instance_Of_The_Scope()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<ITokenProvider, ScopedSignOutProvider>("scoped");
        services.AddTokenProvidersSignOut();
        using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = root.CreateScope();

        await scope.ServiceProvider.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), NullLogger<SignOutTokenProvidersTests>.Instance, CancellationToken.None);

        scope.ServiceProvider.GetRequiredKeyedService<ITokenProvider>("scoped").Should().BeOfType<ScopedSignOutProvider>().Which.SignOuts.Should().Be(1);
    }

    [Fact]
    public async Task SignOutTokenProviders_Should_Rethrow_When_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var provider = new Mock<ISignOutTokenProvider>();
        provider.Setup(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException(cts.Token));
        var container = BuildContainer(services => services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object));

        var exception = await Record.ExceptionAsync(async () => await container.SignOutTokenProvidersAsync(new SimpleKeyValueSettings(), container.GetRequiredService<ILogger<SignOutTokenProvidersTests>>(), cts.Token));

        exception.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task SignOutUser_Should_Sign_Out_Every_Provider_Without_ProviderId()
    {
        var provider = new Mock<ISignOutTokenProvider>();
        var container = BuildContainer(services => services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object));
        var sut = new AppPrincipalFactory(container,
                                          new Mock<INetworkInformation>().Object,
                                          new Mock<ISecureCache>().Object,
                                          new Mock<ICacheKeyGenerator>().Object,
                                          new Mock<IApplicationContext>().Object,
                                          container.GetRequiredService<ILogger<AppPrincipalFactory>>());
        var settings = new SimpleKeyValueSettings();

        await sut.SignOutUserAsync(settings, CancellationToken.None);

        provider.Verify(p => p.SignOutAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutUser_Without_OAuth2_Settings_Should_Sign_Out_Every_Provider()
    {
        var provider = new Mock<ISignOutTokenProvider>();
        var container = BuildContainer(services => services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object));
        var sut = new AppPrincipalFactory(container,
                                          new Mock<INetworkInformation>().Object,
                                          new Mock<ISecureCache>().Object,
                                          new Mock<ICacheKeyGenerator>().Object,
                                          new Mock<IApplicationContext>().Object,
                                          container.GetRequiredService<ILogger<AppPrincipalFactory>>());

        await sut.SignOutUserAsync(CancellationToken.None);

        provider.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Server_SignOutUser_Without_Settings_Should_Sign_Out_Every_Provider()
    {
        var provider = new Mock<ISignOutTokenProvider>();
        var sut = CreateServicePrincipalFactory(services => services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object));

        await sut.SignOutUserAsync(CancellationToken.None);

        provider.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Server_SignOutUser_Should_Sign_Out_The_Providers_When_The_Claims_Cache_Fails()
    {
        var provider = new Mock<ISignOutTokenProvider>();
        var claimsCache = new Mock<ICache>();
        claimsCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("cache down"));
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(claimsCache.Object);
        var keyGenerator = new Mock<ICacheKeyGenerator>();
        keyGenerator.Setup(g => g.GetClaimsKey(It.IsAny<ClaimsIdentity>())).Returns("user_ClaimsCache");

        var sut = CreateServicePrincipalFactory(services =>
        {
            services.AddKeyedSingleton("a", provider.As<ITokenProvider>().Object);
            services.AddSingleton(PrincipalWithToken("token"));
            services.AddSingleton(cacheHelper.Object);
            services.AddSingleton(keyGenerator.Object);
        });

        var exception = await Record.ExceptionAsync(async () => await sut.SignOutUserAsync(new SimpleKeyValueSettings(), CancellationToken.None));

        exception.Should().BeNull();
        provider.Verify(p => p.SignOutAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AppServicePrincipalFactory CreateServicePrincipalFactory(Action<IServiceCollection> register)
    {
        var container = BuildContainer(register);

        return new AppServicePrincipalFactory(container,
                                              container.GetRequiredService<ILogger<AppServicePrincipalFactory>>(),
                                              new Mock<IOptionsMonitor<SimpleKeyValueSettings>>().Object,
                                              new Mock<IClaimsTransformation>().Object,
                                              new Mock<IActivitySourceFactory>().Object);
    }

    private class SingletonSignOutProvider : ISignOutTokenProvider
    {
        public int SignOuts { get; private set; }

        public ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters) => ValueTask.FromResult(Result.Fail<TokenInfo>("No token."));

        public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
        {
            SignOuts++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ScopedSignOutProvider : SingletonSignOutProvider;

    // A service collection whose first enumeration fails (e.g. modified while it is read).
    // Collection<T> rather than List<T>: LINQ enumerates a List<T> directly, without the interface enumerator.
    private sealed class FlakyServiceCollection : System.Collections.ObjectModel.Collection<ServiceDescriptor>, IServiceCollection
    {
        private bool _failed;

        IEnumerator<ServiceDescriptor> IEnumerable<ServiceDescriptor>.GetEnumerator()
        {
            if (!_failed)
            {
                _failed = true;
                throw new InvalidOperationException("Collection was modified.");
            }

            return GetEnumerator();
        }
    }
}
