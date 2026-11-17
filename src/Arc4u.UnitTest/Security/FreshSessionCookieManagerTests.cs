using System.Security.Claims;
using Arc4u.OAuth2.TicketStore;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// A sign-in on the OpenID Connect callback always stores the ticket under a new session key, so a cookie planted in the victim's
/// browser never receives the victim's session (session fixation, Arc4u.Portal #703).
/// </summary>
[Trait("Category", "CI")]
public class FreshSessionCookieManagerTests
{
    private const string CookieName = "__Host-Test";
    private const string CallbackPath = "/signin-oidc";

    [Theory]
    [InlineData("POST", CallbackPath, true)]
    [InlineData("POST", "/SIGNIN-OIDC", true)]
    [InlineData("GET", CallbackPath, false)]
    [InlineData("POST", "/other", false)]
    public void The_cookie_is_hidden_only_on_a_post_to_the_callback(string method, string path, bool hidden)
    {
        // Arrange
        var context = CreateContext(method, path, registerOpenIdConnect: true);
        var sut = new FreshSessionCookieManager(new ChunkingCookieManager());

        // Act
        var value = sut.GetRequestCookie(context, CookieName);

        // Assert
        if (hidden)
        {
            value.Should().BeNull();
        }
        else
        {
            value.Should().Be("value");
        }
    }

    [Fact]
    public void The_cookie_is_read_when_no_OpenID_Connect_options_are_registered()
    {
        // Arrange
        var context = CreateContext("POST", CallbackPath, registerOpenIdConnect: false);
        var sut = new FreshSessionCookieManager(new ChunkingCookieManager());

        // Act
        var value = sut.GetRequestCookie(context, CookieName);

        // Assert
        value.Should().Be("value");
    }

    [Fact]
    public async Task A_sign_in_on_the_callback_never_reuses_the_session_of_a_planted_cookie()
    {
        // Arrange
        var store = new InMemoryTicketStore();
        await using var app = await StartServerAsync(store, freshSession: true);
        var client = app.GetTestClient();
        var plantedCookie = await SignInAsync(client, "/signin/attacker", cookie: null);
        var attackerKey = store.Keys.Single();

        // Act
        var victimCookie = await SignInAsync(client, CallbackPath, plantedCookie);

        // Assert
        victimCookie.Should().NotBe(plantedCookie);
        store.Keys.Should().HaveCount(2);
        store.SubjectOf(attackerKey).Should().Be("attacker");
        store.Keys.Where(k => k != attackerKey).Select(store.SubjectOf).Should().ContainSingle().Which.Should().Be("victim");
    }

    [Fact]
    public async Task Without_the_manager_the_planted_session_receives_the_victim_ticket()
    {
        // Arrange: proves the test above exercises the vulnerability.
        var store = new InMemoryTicketStore();
        await using var app = await StartServerAsync(store, freshSession: false);
        var client = app.GetTestClient();
        var plantedCookie = await SignInAsync(client, "/signin/attacker", cookie: null);
        var attackerKey = store.Keys.Single();

        // Act
        await SignInAsync(client, CallbackPath, plantedCookie);

        // Assert
        store.Keys.Should().ContainSingle();
        store.SubjectOf(attackerKey).Should().Be("victim");
    }

    [Fact]
    public async Task A_sign_in_outside_the_callback_still_renews_the_current_session()
    {
        // Arrange
        var store = new InMemoryTicketStore();
        await using var app = await StartServerAsync(store, freshSession: true);
        var client = app.GetTestClient();
        var cookie = await SignInAsync(client, "/signin/user", cookie: null);

        // Act
        await SignInAsync(client, "/signin/user", cookie);

        // Assert
        store.Keys.Should().ContainSingle();
    }

    private static DefaultHttpContext CreateContext(string method, string path, bool registerOpenIdConnect)
    {
        var services = new ServiceCollection();
        if (registerOpenIdConnect)
        {
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o => o.CallbackPath = CallbackPath);
        }

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Cookie = $"{CookieName}=value";
        return context;
    }

    private static async Task<WebApplication> StartServerAsync(ITicketStore store, bool freshSession)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o => o.CallbackPath = CallbackPath);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.SessionStore = store;
                o.Cookie.Name = CookieName;
                if (freshSession)
                {
                    o.CookieManager = new FreshSessionCookieManager(new ChunkingCookieManager());
                }
            });

        var app = builder.Build();
        app.UseAuthentication();
        // Stands in for the OpenID Connect handler, which signs the user in on the callback.
        app.MapPost(CallbackPath, (HttpContext context) => context.SignInAsync(Principal("victim")));
        app.MapPost("/signin/{user}", (string user, HttpContext context) => context.SignInAsync(Principal(user)));

        await app.StartAsync();
        return app;
    }

    private static ClaimsPrincipal Principal(string subject)
        => new(new ClaimsIdentity([new Claim("sub", subject)], CookieAuthenticationDefaults.AuthenticationScheme));

    // Posts with the given cookie and returns the authentication cookie set by the response ("name=value").
    private static async Task<string> SignInAsync(HttpClient client, string path, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (cookie is not null)
        {
            request.Headers.Add(HeaderNames.Cookie, cookie);
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var setCookie = response.Headers.GetValues(HeaderNames.SetCookie).Single(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }

    private sealed class InMemoryTicketStore : ITicketStore
    {
        private readonly Dictionary<string, AuthenticationTicket> _tickets = [];

        public IReadOnlyCollection<string> Keys => _tickets.Keys;

        public string? SubjectOf(string key) => _tickets[key].Principal.FindFirst("sub")?.Value;

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString();
            _tickets[key] = ticket;
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            _tickets[key] = ticket;
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket?> RetrieveAsync(string key)
            => Task.FromResult(_tickets.GetValueOrDefault(key));

        public Task RemoveAsync(string key)
        {
            _tickets.Remove(key);
            return Task.CompletedTask;
        }
    }
}
