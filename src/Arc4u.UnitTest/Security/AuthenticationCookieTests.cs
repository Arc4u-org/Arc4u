using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    }

    private static ServiceProvider CreateProvider(bool useTicketStore, Action<IServiceCollection>? replaceServices = null)
    {
        var ecdsa = ECDsa.Create();
        var certificate = new CertificateRequest("cn=TestCertificate", ecdsa, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        IServiceCollection services = new ServiceCollection();

        services.AddOidcAuthentication(options =>
        {
            options.DefaultAuthority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);
            options.CookieName = CookieName;
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
