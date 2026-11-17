using System.Security.Claims;
using Arc4u.Blazor;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.UnitTest.Blazor;

/// <summary>
/// The authentication state written into the page for the WebAssembly client carries only the claims the application lists, besides the
/// name and role claims, instead of every claim of the user (<c>SerializeAllClaims</c>); the session key never leaves the server.
/// </summary>
[Trait("Category", "CI")]
public sealed class AuthenticationStateClaimsSerializerTests
{
    [Fact]
    public async Task Nothing_is_sent_for_an_anonymous_user()
    {
        // Act
        var data = await AuthenticationStateClaimsSerializer.Create(ClaimTypes.Email)(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        // Assert
        data.Should().BeNull();
    }

    [Fact]
    public async Task The_name_the_roles_and_the_listed_claims_are_sent_and_nothing_else()
    {
        // Arrange
        var state = State(
            (ClaimTypes.Name, "alice"),
            (ClaimTypes.Role, "admin"),
            (ClaimTypes.Role, "user"),
            (ClaimTypes.Email, "alice@example.com"),
            ("sid", "secret-sid"),
            (Arc4uClaimTypes.Authorization, "{}"));

        // Act
        var data = await AuthenticationStateClaimsSerializer.Create(ClaimTypes.Email)(state);

        // Assert
        data!.NameClaimType.Should().Be(ClaimTypes.Name);
        data.RoleClaimType.Should().Be(ClaimTypes.Role);
        data.Claims.Select(c => (c.Type, c.Value)).Should().Equal(
            (ClaimTypes.Name, "alice"),
            (ClaimTypes.Role, "admin"),
            (ClaimTypes.Role, "user"),
            (ClaimTypes.Email, "alice@example.com"));
    }

    [Fact]
    public async Task The_session_key_is_never_sent_even_when_listed()
    {
        // Arrange
        var state = State((ClaimTypes.Name, "alice"), (Arc4uClaimTypes.SessionKey, "AuthSessionStore-123"));

        // Act
        var data = await AuthenticationStateClaimsSerializer.Create(Arc4uClaimTypes.SessionKey)(state);

        // Assert
        data!.Claims.Should().NotContain(c => c.Type == Arc4uClaimTypes.SessionKey);
    }

    [Fact]
    public async Task A_listed_name_or_role_claim_type_is_not_sent_twice()
    {
        // Arrange
        var state = State((ClaimTypes.Name, "alice"), (ClaimTypes.Role, "admin"));

        // Act
        var data = await AuthenticationStateClaimsSerializer.Create(ClaimTypes.Name, ClaimTypes.Role)(state);

        // Assert
        data!.Claims.Should().HaveCount(2);
    }

    private static AuthenticationState State(params (string Type, string Value)[] claims)
        => new(new ClaimsPrincipal(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Cookies")));
}
