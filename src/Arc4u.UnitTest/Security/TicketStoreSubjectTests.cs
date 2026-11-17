using System.Security.Claims;
using Arc4u.Caching;
using Arc4u.OAuth2.TicketStore;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The ticket stores never overwrite the session of one user with the ticket of another, which would hand that session to whoever
/// holds the cookie (session fixation, Arc4u.Portal #703). A renewal for the same user still replaces the ticket.
/// </summary>
[Trait("Category", "CI")]
public sealed class TicketStoreSubjectTests : IDisposable
{
    private readonly ServiceProvider _container = SignOutTestSupport.BuildContainer(_ => { });
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("arc4u-tickets-");

    public void Dispose()
    {
        _container.Dispose();
        _directory.Delete(recursive: true);
    }

    public static TheoryData<string> Stores => ["cache", "file"];

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_renewal_for_another_user_is_refused_and_keeps_the_stored_ticket(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        var key = await sut.StoreAsync(Ticket(("sub", "attacker")));

        // Act
        var act = () => sut.RenewAsync(key, Ticket(("sub", "victim")));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await sut.RetrieveAsync(key))!.Principal.FindFirst("sub")!.Value.Should().Be("attacker");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_renewal_for_the_same_user_replaces_the_ticket(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        var key = await sut.StoreAsync(Ticket(("sub", "user"), ("name", "before")));

        // Act
        await sut.RenewAsync(key, Ticket(("sub", "user"), ("name", "after")));

        // Assert
        (await sut.RetrieveAsync(key))!.Principal.FindFirst("name")!.Value.Should().Be("after");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_renewal_of_an_expired_session_writes_the_ticket(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);

        // Act
        await sut.RenewAsync("missing", Ticket(("sub", "user")));

        // Assert
        (await sut.RetrieveAsync("missing"))!.Principal.FindFirst("sub")!.Value.Should().Be("user");
    }

    [Theory]
    [InlineData("oid")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public async Task Any_identifier_claim_tells_the_users_apart(string claimType)
    {
        // Arrange
        var sut = CreateStore("cache");
        var key = await sut.StoreAsync(Ticket((claimType, "one")));

        // Act
        var act = () => sut.RenewAsync(key, Ticket((claimType, "two")));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Tickets_without_a_common_identifier_are_not_blocked()
    {
        // Arrange
        var sut = CreateStore("cache");
        var key = await sut.StoreAsync(Ticket(("sub", "user")));

        // Act
        var act = () => sut.RenewAsync(key, Ticket(("oid", "user")));

        // Assert
        await act.Should().NotThrowAsync();
    }

    private ITicketStore CreateStore(string kind) => kind switch
    {
        "cache" => new CacheTicketStore(
            _container.GetRequiredService<ILogger<CacheTicketStore>>(),
            CreateCacheContext(),
            Mock.Of<IOptionsMonitor<CacheTicketStoreOptions>>(m => m.CurrentValue == new CacheTicketStoreOptions())),
        "file" => new FileTicketStore(
            _container.GetRequiredService<ILogger<FileTicketStore>>(),
            Mock.Of<IOptionsMonitor<FileTicketStoreOptions>>(m => m.CurrentValue == new FileTicketStoreOptions { StorePath = _directory })),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    // A cache backed by a dictionary.
    private static ICacheContext CreateCacheContext()
    {
        var entries = new Dictionary<string, byte[]>();
        var cache = new Mock<ICache>();
        cache.Setup(c => c.GetAsync<byte[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string key, CancellationToken _) => entries.GetValueOrDefault(key));
        cache.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
             .Callback((string key, TimeSpan _, byte[] value, bool _, CancellationToken _) => entries[key] = value)
             .Returns(Task.CompletedTask);

        var context = new Mock<ICacheContext>();
        context.Setup(c => c.Exist(It.IsAny<string>())).Returns(false);
        context.SetupGet(c => c.Default).Returns(cache.Object);
        return context.Object;
    }

    private static AuthenticationTicket Ticket(params (string Type, string Value)[] claims)
        => new(new ClaimsPrincipal(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Cookies")), "Cookies");
}
