using System.Security.Claims;
using Arc4u.Blazor;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Xunit;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.UnitTest.Blazor;

/// <summary>
/// The session store hands back tickets whose principal carries their own session key, so a Blazor circuit, which only sees the principal,
/// can check that its sign-in still exists. The key is never persisted with the ticket.
/// </summary>
[Trait("Category", "CI")]
public sealed class SessionKeyTicketStoreTests
{
    private readonly InMemoryTicketStore _inner = new();

    [Fact]
    public async Task A_retrieved_ticket_carries_its_session_key()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);
        var key = await sut.StoreAsync(Ticket());

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        ticket!.Principal.FindAll(Arc4uClaimTypes.SessionKey).Select(c => c.Value).Should().Equal(key);
    }

    [Fact]
    public async Task The_stored_ticket_is_not_modified_by_a_retrieval()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);
        var key = await sut.StoreAsync(Ticket());

        // Act
        await sut.RetrieveAsync(key);

        // Assert
        _inner.Tickets[key].Principal.HasClaim(c => c.Type == Arc4uClaimTypes.SessionKey).Should().BeFalse();
    }

    [Fact]
    public async Task A_session_key_on_a_ticket_is_never_stored_or_renewed()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);
        var key = await sut.StoreAsync(Ticket((Arc4uClaimTypes.SessionKey, "forged")));
        var retrieved = await sut.RetrieveAsync(key);

        // Act
        await sut.RenewAsync(key, retrieved!);

        // Assert
        _inner.Tickets[key].Principal.HasClaim(c => c.Type == Arc4uClaimTypes.SessionKey).Should().BeFalse();
    }

    [Fact]
    public async Task A_stale_session_key_is_replaced_on_retrieval()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);
        _inner.Tickets["the-key"] = Ticket((Arc4uClaimTypes.SessionKey, "another-key"));

        // Act
        var ticket = await sut.RetrieveAsync("the-key");

        // Assert
        ticket!.Principal.FindAll(Arc4uClaimTypes.SessionKey).Select(c => c.Value).Should().Equal("the-key");
    }

    [Fact]
    public async Task A_missing_ticket_is_still_missing()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);

        // Act
        var ticket = await sut.RetrieveAsync("unknown");

        // Assert
        ticket.Should().BeNull();
    }

    [Fact]
    public async Task Removal_is_passed_through()
    {
        // Arrange
        var sut = new SessionKeyTicketStore(_inner);
        var key = await sut.StoreAsync(Ticket());

        // Act
        await sut.RemoveAsync(key, CancellationToken.None);

        // Assert
        (await sut.RetrieveAsync(key)).Should().BeNull();
    }

    [Fact]
    public void Wrapping_is_idempotent_and_skips_a_cookie_without_session_store()
    {
        // Arrange
        var withStore = new CookieAuthenticationOptions { SessionStore = _inner };
        var withoutStore = new CookieAuthenticationOptions();

        // Act
        SessionKeyTicketStore.Wrap(withStore);
        var first = withStore.SessionStore;
        SessionKeyTicketStore.Wrap(withStore);
        SessionKeyTicketStore.Wrap(withoutStore);

        // Assert
        withStore.SessionStore.Should().BeSameAs(first).And.BeOfType<SessionKeyTicketStore>().Which.Inner.Should().BeSameAs(_inner);
        withoutStore.SessionStore.Should().BeNull();
    }

    internal static AuthenticationTicket Ticket(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity([new Claim("sub", "user"), .. claims.Select(c => new Claim(c.Type, c.Value))], "Cookies");
        return new AuthenticationTicket(new ClaimsPrincipal(identity), CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>A ticket store keeping the tickets as they are given, so the tests can see what was persisted.</summary>
    internal sealed class InMemoryTicketStore : ITicketStore
    {
        public Dictionary<string, AuthenticationTicket> Tickets { get; } = [];

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString();
            Tickets[key] = ticket;
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            Tickets[key] = ticket;
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket?> RetrieveAsync(string key)
            => Task.FromResult(Tickets.TryGetValue(key, out var ticket) ? ticket : null);

        public Task RemoveAsync(string key)
        {
            Tickets.Remove(key);
            return Task.CompletedTask;
        }
    }
}
