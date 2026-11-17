using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.TicketStore;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The cookies issued by the OpenID Connect authentication are sent over HTTPS only and cannot be read from JavaScript;
/// the authentication cookie is also not sent on cross-site sub-requests (issue #110).
/// </summary>
[Trait("Category", "CI")]
public class AuthenticationCookieTests
{
    private const string CookieName = ".Arc4u.Test.Cookies";

    [Fact]
    public void Authentication_Cookie_Without_Ticket_Store_Should_Be_Secure_HttpOnly_And_SameSite_Lax()
    {
        var provider = CreateProvider(useTicketStore: false);

        var sut = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        sut.SessionStore.Should().BeNull();
        ShouldBeRestricted(sut.Cookie);
    }

    [Fact]
    public void Authentication_Cookie_With_Ticket_Store_Should_Be_Secure_HttpOnly_And_SameSite_Lax()
    {
        var ticketStore = Mock.Of<ITicketStore>();
        var provider = CreateProvider(useTicketStore: true, services => services.AddSingleton(ticketStore));

        var sut = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        sut.SessionStore.Should().BeSameAs(ticketStore);
        ShouldBeRestricted(sut.Cookie);
    }

    /// <summary>A sign-in must never renew the session key of a cookie the browser already holds (Arc4u.Portal #703).</summary>
    [Fact]
    public void Authentication_Cookie_With_Ticket_Store_Should_Start_A_Fresh_Session_On_Sign_In()
    {
        var provider = CreateProvider(useTicketStore: true, services => services.AddSingleton(Mock.Of<ITicketStore>()));

        var sut = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        sut.CookieManager.Should().BeOfType<FreshSessionCookieManager>()
            .Which.Inner.Should().BeOfType<ChunkingCookieManager>();
    }

    [Theory]
    [InlineData(CookieName, true)]
    [InlineData("__Host-Arc4u", false)]
    public void A_Cookie_Name_Without_The_Host_Prefix_Should_Be_Warned_About(string cookieName, bool warned)
    {
        var logger = new CollectingLogger<ConfigureStandardCookieAuthenticationOptions>();
        var provider = CreateProvider(useTicketStore: false, services => services.AddSingleton<ILogger<ConfigureStandardCookieAuthenticationOptions>>(logger), cookieName);

        _ = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        logger.EventIds.Contains(9152).Should().Be(warned);
    }

    [Theory]
    [InlineData(CookieName, false, CookieName)]
    [InlineData(CookieName, true, "__Host-" + CookieName)]
    [InlineData("__Host-Arc4u", true, "__Host-Arc4u")]
    public void The_Host_Prefix_Should_Be_Added_To_The_Cookie_Name_Only_On_Request(string cookieName, bool addHostPrefix, string expected)
    {
        var logger = new CollectingLogger<ConfigureCookieWithTicketStoreAuthenticationOptions>();
        var provider = CreateProvider(useTicketStore: true, services =>
        {
            services.AddSingleton(Mock.Of<ITicketStore>());
            services.AddSingleton<ILogger<ConfigureCookieWithTicketStoreAuthenticationOptions>>(logger);
        }, cookieName, addHostPrefix);

        var sut = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        sut.Cookie.Name.Should().Be(expected);
        logger.EventIds.Contains(9152).Should().Be(!expected.StartsWith("__Host-", StringComparison.Ordinal));
    }

    /// <summary>The issued cookie carries <c>path=/</c> even under a path base, or browsers would drop the <c>__Host-</c> cookie (Arc4u.Portal #714).</summary>
    [Fact]
    public async Task Authentication_Cookie_Should_Be_Issued_For_The_Root_Path_Under_A_Path_Base()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<CookieAuthenticationEvents>();
        builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>>(new ConfigureStandardCookieAuthenticationOptions(
            Mock.Of<IOptionsMonitor<OidcAuthenticationOptions>>(m => m.CurrentValue == new OidcAuthenticationOptions { CookieName = "__Host-Arc4u" })));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        await using var app = builder.Build();
        app.UsePathBase("/x");
        app.MapPost("/signin", (HttpContext context) => context.SignInAsync(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user")], CookieAuthenticationDefaults.AuthenticationScheme))));
        await app.StartAsync();

        using var response = await app.GetTestClient().PostAsync(new Uri("https://localhost/x/signin"), null);

        response.EnsureSuccessStatusCode();
        var cookie = response.Headers.GetValues(HeaderNames.SetCookie).Single(c => c.StartsWith("__Host-Arc4u=", StringComparison.Ordinal));
        cookie.Split(';', StringSplitOptions.TrimEntries).Should().Contain("path=/").And.NotContain(p => p.StartsWith("domain=", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<int> EventIds { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => EventIds.Add(eventId.Id);
    }

    [Fact]
    public void OpenIdConnect_Correlation_And_Nonce_Cookies_Should_Be_Secure_And_HttpOnly()
    {
        var provider = CreateProvider(useTicketStore: false);

        var sut = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

        // SameSite=None is required: the identity provider posts the response back to the application from another site.
        foreach (var cookie in new[] { sut.CorrelationCookie, sut.NonceCookie })
        {
            cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
            cookie.HttpOnly.Should().BeTrue();
            cookie.SameSite.Should().Be(SameSiteMode.None);
        }
    }

    private static void ShouldBeRestricted(CookieBuilder cookie)
    {
        cookie.Name.Should().Be(CookieName);
        cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        cookie.HttpOnly.Should().BeTrue();
        cookie.SameSite.Should().Be(SameSiteMode.Lax);
        cookie.IsEssential.Should().BeTrue();
        // What a __Host- cookie requires; a path taken from the path base would make browsers drop it (Arc4u.Portal #714).
        cookie.Path.Should().Be("/");
        cookie.Domain.Should().BeNull();
    }

    private static ServiceProvider CreateProvider(bool useTicketStore, Action<IServiceCollection>? replaceServices = null, string cookieName = CookieName, bool addHostPrefix = false)
    {
        var ecdsa = ECDsa.Create();
        var certificate = new CertificateRequest("cn=TestCertificate", ecdsa, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        IServiceCollection services = new ServiceCollection();

        services.AddOidcAuthentication(options =>
        {
            options.DefaultAuthority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);
            options.CookieName = cookieName;
            options.AddHostPrefixToCookieName = addHostPrefix;
            options.ApplicationName = "TestName";
            options.DataProtectionCertificate = certificate;
            options.DataProtectionCacheStoreOption = o => { o.CacheKey = "Key"; o.CacheName = "Name"; };
            options.ClaimsIdentifierOptions = o => o.Add("oid");
            options.OpenIdSettingsOptions = o =>
            {
                o.ClientId = "ClientId";
                o.ClientSecret = "Secret";
                o.Audiences = ["ClientId"];
                o.Scopes = ["openid"];
            };
            if (useTicketStore)
            {
                options.AuthenticationCacheTicketStoreOption = o => { o.CacheName = "Name"; o.KeyPrefix = "Prefix-"; };
            }
        });

        // The keys are persisted in a cache: an ephemeral provider avoids registering one to read the options.
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        replaceServices?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
