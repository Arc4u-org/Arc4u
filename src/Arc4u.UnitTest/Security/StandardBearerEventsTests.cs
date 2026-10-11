using System.Text.Json;
using Arc4u.OAuth2.Events;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class StandardBearerEventsTests
{
    [Fact]
    public async Task Challenge_Should_Write_A_401_Problem_Details()
    {
        var (context, body) = CreateContext();
        context.Error = "invalid_token";

        await new StandardBearerEvents(NullLogger<StandardBearerEvents>.Instance).Challenge(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.ContentType.Should().Be("application/problem+json");

        using var json = JsonDocument.Parse(body.ToArray());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status401Unauthorized);
        json.RootElement.GetProperty("title").GetString().Should().Be("invalid_token");
    }

    [Fact]
    public async Task Challenge_With_An_Expired_Token_Should_Give_The_Expiration_Date()
    {
        var expires = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var (context, body) = CreateContext(new SecurityTokenExpiredException("expired") { Expires = expires });

        await new StandardBearerEvents(NullLogger<StandardBearerEvents>.Instance).Challenge(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers["x-token-expired"].ToString().Should().Be(expires.ToString("o"));

        using var json = JsonDocument.Parse(body.ToArray());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status401Unauthorized);
        json.RootElement.GetProperty("detail").GetString().Should().Be($"The token expired on {expires:o}");
    }

    private static (JwtBearerChallengeContext Context, MemoryStream Body) CreateContext(Exception? failure = null)
    {
        var body = new MemoryStream();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = body;

        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var context = new JwtBearerChallengeContext(httpContext, scheme, new JwtBearerOptions(), new AuthenticationProperties())
        {
            AuthenticateFailure = failure
        };

        return (context, body);
    }
}
