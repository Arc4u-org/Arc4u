using System.Diagnostics;
using System.Security.Claims;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Middleware;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class AddContextToPrincipalMiddlewareTests
{
    private static DefaultHttpContext BuildContext()
    {
        return new DefaultHttpContext
        {
            User = new AppPrincipal(new Arc4u.Security.Principal.Authorization(), new ClaimsIdentity("TestAuth"), "S-1-0-0") { Profile = UserProfile.Empty }
        };
    }

    // Issue #124: no listener means no Activity.Current, the middleware must not throw.
    [Fact]
    public async Task InvokeAsync_Without_Activity_Does_Not_Throw_And_Calls_Next()
    {
        var factory = new DefaultActivitySourceFactory();
        var nextCalled = false;
        var sut = new AddContextToPrincipalMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, factory);
        var context = BuildContext();

        Activity.Current = null;
        var act = () => sut.InvokeAsync(context, NullLogger<AddContextToPrincipalMiddleware>.Instance);

        await act.Should().NotThrowAsync();
        nextCalled.Should().BeTrue();
        context.Request.Headers.ContainsKey("traceparent").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_With_Activity_Adds_Traceparent()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Arc4u",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        var sut = new AddContextToPrincipalMiddleware(_ => Task.CompletedTask, new DefaultActivitySourceFactory());
        var context = BuildContext();

        await sut.InvokeAsync(context, NullLogger<AddContextToPrincipalMiddleware>.Instance);

        context.Request.Headers["traceparent"].ToString().Should().NotBeNullOrEmpty();
    }
}
