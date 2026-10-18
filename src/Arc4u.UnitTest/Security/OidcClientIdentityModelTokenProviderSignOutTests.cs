using System.Net;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.OAuth2.Client.Authentication.Options;
using Arc4u.OAuth2.Client.Authentication.TokenProvider;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using FluentResults;
using BrowserOptions = Duende.IdentityModel.OidcClient.Browser.BrowserOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>The sign-out of <see cref="OidcClientIdentityModelTokenProvider"/> and its behavior around it: concurrent calls, timeouts, secure-cache and refresh failures (issue #113).</summary>
[Trait("Category", "CI")]
public class OidcClientIdentityModelTokenProviderSignOutTests
{
    [Fact]
    public async Task OidcClient_SignOut_Should_Send_The_Identity_Token_Of_The_Session_As_Hint()
    {
        // The session is read from the secure cache, with the identity token of its login.
        var oidc = CreateOidcClient(null, null);
        oidc.SecureCache.Setup(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new TokenRefreshInfo
                        {
                            AccessToken = new TokenInfo("Bearer", "access", DateTime.UtcNow.AddHours(1)),
                            RefreshToken = new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)),
                            IdentityToken = "the-identity-token"
                        });
        string? logoutUrl = null;
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Callback<BrowserOptions, CancellationToken>((o, _) => logoutUrl = o.StartUrl)
                    .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.Success, Response = "myapp://callback" });

        (await oidc.Provider.GetTokenAsync(null, null)).IsSuccess.Should().BeTrue();
        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        logoutUrl.Should().Contain("id_token_hint=the-identity-token");
        oidc.Tokens.IdentityToken.Should().BeNull();
    }

    [Fact]
    public async Task OidcClient_SignOut_Of_A_Session_Only_In_The_Cache_Should_Send_Its_Identity_Token_As_Hint()
    {
        var oidc = CreateOidcClient(null, null);
        oidc.SecureCache.Setup(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new TokenRefreshInfo { RefreshToken = new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)), IdentityToken = "the-identity-token" });
        string? logoutUrl = null;
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Callback<BrowserOptions, CancellationToken>((o, _) => logoutUrl = o.StartUrl)
                    .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.Success, Response = "myapp://callback" });

        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        logoutUrl.Should().Contain("id_token_hint=the-identity-token");
    }

    [Fact]
    public async Task OidcClient_SignOut_Should_Clear_The_Tokens_In_Memory_And_Log_Out()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddHours(1)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));

        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        oidc.Tokens.AccessToken.Should().BeNull();
        oidc.Tokens.RefreshToken.Should().BeNull();
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OidcClient_SignOut_Without_Session_Should_Not_Open_The_Browser()
    {
        var oidc = CreateOidcClient(null, null);

        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_Refresh_In_Progress_During_SignOut_Should_Not_Be_Kept()
    {
        // The access token is in the refresh threshold: GetTokenAsync refreshes it, and the token endpoint waits for the gate.
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));

        var inFlight = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);
        oidc.TokenEndpoint.Gate.SetResult();

        (await inFlight).IsFailed.Should().BeTrue();
        oidc.Tokens.AccessToken.Should().BeNull();
        oidc.Tokens.RefreshToken.Should().BeNull();
        oidc.SecureCache.Verify(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TokenRefreshInfo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_SignOut_During_The_Write_Of_Refreshed_Tokens_Should_Remove_Them()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));
        oidc.TokenEndpoint.Gate.SetResult();

        // The refreshed tokens passed the sign-out check and are being written to the secure cache.
        var putStarted = new TaskCompletionSource();
        var putGate = new TaskCompletionSource();
        oidc.SecureCache.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TokenRefreshInfo>(), It.IsAny<CancellationToken>()))
                        .Returns(() =>
                        {
                            putStarted.SetResult();
                            return putGate.Task;
                        });

        var inFlight = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await putStarted.Task;
        var signOut = oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None).AsTask();

        signOut.IsCompleted.Should().BeFalse("the sign-out waits for the write in progress");
        putGate.SetResult();
        await inFlight;
        await signOut;

        oidc.Tokens.AccessToken.Should().BeNull();
        oidc.Tokens.RefreshToken.Should().BeNull();
        oidc.SecureCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OidcClient_Concurrent_Calls_Should_Refresh_The_Token_Once()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));

        var first = oidc.Provider.GetTokenAsync(null, null).AsTask();
        var second = oidc.Provider.GetTokenAsync(null, null).AsTask();
        oidc.TokenEndpoint.Gate.SetResult();

        (await first).Value.Token.Should().Be("new-access");
        (await second).Value.Token.Should().Be("new-access");
        oidc.TokenEndpoint.Calls.Should().Be(1);
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_Tokens_Read_From_The_Cache_During_SignOut_Should_Not_Be_Kept()
    {
        var oidc = CreateOidcClient(null, null);
        var read = new TaskCompletionSource<TokenRefreshInfo?>();
        oidc.SecureCache.SetupSequence(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Returns(read.Task)
                        .ReturnsAsync((TokenRefreshInfo?)null);

        var inFlight = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);
        read.SetResult(new TokenRefreshInfo { AccessToken = new TokenInfo("Bearer", "cached", DateTime.UtcNow.AddHours(1)) });

        (await inFlight).IsFailed.Should().BeTrue();
        oidc.Tokens.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task OidcClient_Login_Not_Completed_In_Time_Should_Fail()
    {
        var oidc = CreateOidcClient(null, null, loginTimeout: TimeSpan.FromMilliseconds(200));
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<BrowserOptions, CancellationToken>(async (_, ct) =>
                    {
                        // The user never completes the login.
                        await Task.Delay(Timeout.Infinite, ct);
                        return new BrowserResult();
                    });

        var result = await oidc.Provider.GetTokenAsync(null, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("did not complete");
    }

    [Fact]
    public async Task OidcClient_Call_Waiting_Longer_Than_The_Login_Timeout_Should_Fail()
    {
        // The browser ignores the cancellation: the login in progress never ends, the next call waits at most LoginTimeout.
        var oidc = CreateOidcClient(null, null, loginTimeout: TimeSpan.FromMilliseconds(200));
        var browser = new TaskCompletionSource<BrowserResult>();
        var browserOpened = new TaskCompletionSource();
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        browserOpened.TrySetResult();
                        return browser.Task;
                    });

        // The first call holds the lock once it has opened the browser.
        var login = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await browserOpened.Task;
        var waiting = await oidc.Provider.GetTokenAsync(null, null);

        waiting.IsFailed.Should().BeTrue();
        waiting.Errors.Should().ContainSingle().Which.Message.Should().Contain("another call");

        browser.SetResult(new BrowserResult { ResultType = BrowserResultType.UserCancel });
        (await login).IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task OidcClient_Calls_Queued_Behind_A_Cancelled_Login_Should_Not_Open_The_Browser_Again()
    {
        var oidc = CreateOidcClient(null, null);
        var browser = new TaskCompletionSource<BrowserResult>();
        var browserOpened = new TaskCompletionSource();
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        browserOpened.TrySetResult();
                        return browser.Task;
                    });

        var login = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await browserOpened.Task;
        var queued = new[] { oidc.Provider.GetTokenAsync(null, null).AsTask(), oidc.Provider.GetTokenAsync(null, null).AsTask() };

        // The user cancels the login: the queued calls fail with it instead of logging in again.
        browser.SetResult(new BrowserResult { ResultType = BrowserResultType.UserCancel });

        var results = new List<Result<TokenInfo>> { await login };
        foreach (var call in queued)
        {
            results.Add(await call);
        }

        results.Should().OnlyContain(r => r.IsFailed);
        // Each call gets its own (mutable) result.
        results.Should().OnlyHaveUniqueItems(r => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(r));
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OidcClient_Calls_Queued_Behind_A_Refresh_Should_Get_Its_Token()
    {
        // Short-lived tokens: the refreshed token is already inside the refresh threshold, the queued call still gets it.
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));
        oidc.TokenEndpoint.ExpiresIn = 60;

        var first = oidc.Provider.GetTokenAsync(null, null).AsTask();
        var second = oidc.Provider.GetTokenAsync(null, null).AsTask();
        oidc.TokenEndpoint.Gate.SetResult();

        (await first).Value.Token.Should().Be("new-access");
        (await second).Value.Token.Should().Be("new-access");
        oidc.TokenEndpoint.Calls.Should().Be(1);
    }

    [Fact]
    public async Task OidcClient_Failing_Secure_Cache_Write_Should_Keep_The_Tokens_And_Log()
    {
        var logger = new Mock<ILogger<OidcClientIdentityModelTokenProvider>>();
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)), logger: logger.Object);
        oidc.TokenEndpoint.Gate.SetResult();
        oidc.SecureCache.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TokenRefreshInfo>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new IOException("The file is locked."));

        var result = await oidc.Provider.GetTokenAsync(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-access");
        VerifyErrorLogged(logger);
    }

    [Fact]
    public async Task OidcClient_Stuck_Secure_Cache_Write_Should_Keep_The_Tokens_In_Memory()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)), secureCacheTimeout: TimeSpan.FromMilliseconds(100));
        oidc.TokenEndpoint.Gate.SetResult();
        oidc.SecureCache.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TokenRefreshInfo>(), It.IsAny<CancellationToken>()))
                        .Returns<string, TokenRefreshInfo, CancellationToken>((_, _, ct) => Task.Delay(Timeout.Infinite, ct));

        var result = await oidc.Provider.GetTokenAsync(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-access");
        oidc.Tokens.AccessToken!.Token.Should().Be("new-access");
    }

    [Fact]
    public async Task OidcClient_SignOut_On_A_Stuck_Secure_Cache_Should_End()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddHours(1)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)), secureCacheTimeout: TimeSpan.FromMilliseconds(100));
        oidc.SecureCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Returns<string, CancellationToken>(async (_, ct) =>
                        {
                            await Task.Delay(Timeout.Infinite, ct);
                            return true;
                        });

        var exception = await Record.ExceptionAsync(async () => await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None));

        // The removal timed out and is logged, but the tokens in memory are cleared and the user is still logged out of the authority.
        exception.Should().BeNull();
        oidc.Tokens.AccessToken.Should().BeNull();
        oidc.Tokens.RefreshToken.Should().BeNull();
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyErrorLogged(oidc.Logger);
    }

    [Fact]
    public async Task OidcClient_Logout_Not_Completed_In_Time_Should_End_The_SignOut()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddHours(1)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)), loginTimeout: TimeSpan.FromMilliseconds(200));
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<BrowserOptions, CancellationToken>(async (_, ct) =>
                    {
                        // The user leaves the end-session page open.
                        await Task.Delay(Timeout.Infinite, ct);
                        return new BrowserResult();
                    });

        var exception = await Record.ExceptionAsync(async () => await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(10)));

        exception.Should().BeNull();
        oidc.Tokens.AccessToken.Should().BeNull();
        VerifyErrorLogged(oidc.Logger);
    }

    [Fact]
    public async Task OidcClient_SignOut_Should_Cancel_A_Login_In_Progress()
    {
        var oidc = CreateOidcClient(null, null);
        var browserOpened = new TaskCompletionSource();
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<BrowserOptions, CancellationToken>(async (_, ct) =>
                    {
                        browserOpened.TrySetResult();
                        await Task.Delay(Timeout.Infinite, ct);
                        return new BrowserResult();
                    });

        var login = oidc.Provider.GetTokenAsync(null, null).AsTask();
        await browserOpened.Task;
        await oidc.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        // The login window is closed by the sign-out, without waiting for LoginTimeout (5 minutes).
        var result = await login.WaitAsync(TimeSpan.FromSeconds(10));
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("signed out");
    }

    [Fact]
    public async Task OidcClient_Transient_Cache_Read_Failure_Should_Fail_And_Keep_The_Entry()
    {
        foreach (var failure in new Exception[] { new IOException("The file is locked."), new TaskCanceledException("The cache timed out.") })
        {
            var oidc = CreateOidcClient(null, null);
            oidc.SecureCache.Setup(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

            var result = await oidc.Provider.GetTokenAsync(null, null);

            // The call fails, but the session in the cache is kept and the user is not asked to log in.
            result.IsFailed.Should().BeTrue();
            oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            oidc.SecureCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyErrorLogged(oidc.Logger);
        }
    }

    [Fact]
    public async Task OidcClient_Network_Error_During_A_Refresh_Should_Keep_The_Session()
    {
        // The access token is in the refresh threshold but not expired: it is still returned.
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));
        oidc.TokenEndpoint.Failure = new HttpRequestException("The network is unreachable.");
        oidc.TokenEndpoint.Gate.SetResult();

        var result = await oidc.Provider.GetTokenAsync(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("access");
        oidc.Tokens.RefreshToken!.Token.Should().Be("refresh");
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_Network_Error_With_An_Expired_Access_Token_Should_Fail_And_Keep_The_Refresh_Token()
    {
        var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(-1)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));
        oidc.TokenEndpoint.Failure = new HttpRequestException("The network is unreachable.");
        oidc.TokenEndpoint.Gate.SetResult();

        var result = await oidc.Provider.GetTokenAsync(null, null);

        result.IsFailed.Should().BeTrue();
        oidc.Tokens.RefreshToken!.Token.Should().Be("refresh");
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_Rejected_Refresh_Token_Should_Log_The_User_In()
    {
        // invalid_grant: revoked or expired; invalid_scope: a scope added since the login was not granted to the refresh token.
        foreach (var error in new[] { "invalid_grant", "invalid_scope" })
        {
            var oidc = CreateOidcClient(new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2)), new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddHours(8)));
            oidc.TokenEndpoint.Error = error;
            oidc.TokenEndpoint.Gate.SetResult();
            oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.UserCancel });

            await oidc.Provider.GetTokenAsync(null, null);

            oidc.Tokens.RefreshToken.Should().BeNull(error);
            oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task OidcClient_Inaccessible_Secure_Cache_Should_Log_The_User_In_And_Keep_The_Entry()
    {
        foreach (var failure in new Exception[] { new UnauthorizedAccessException("Access denied."), new DirectoryNotFoundException("No folder."), new NotSupportedException("TokenRefreshInfo is not in the JsonSerializerContext.") })
        {
            var oidc = CreateOidcClient(null, null);
            oidc.SecureCache.Setup(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
            oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.UserCancel });

            var exception = await Record.ExceptionAsync(async () => await oidc.Provider.GetTokenAsync(null, null));

            // The application can work (the user logs in), and the entry, which may be valid, is not removed.
            exception.Should().BeNull();
            oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            oidc.SecureCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyErrorLogged(oidc.Logger);
        }
    }

    [Fact]
    public async Task OidcClient_SignOut_Before_The_Login_Starts_Should_Not_Open_The_Browser()
    {
        // The sign-out runs after the call took the lock and checked the sign-out, while it prepares the login request (it reads the extra parameters).
        OidcClientSetup? oidc = null;
        var signedOut = false;
        oidc = CreateOidcClient(null, null, onExtraContextRead: () =>
        {
            if (!signedOut)
            {
                signedOut = true;
                oidc!.Provider.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            }
        });

        var result = await oidc.Provider.GetTokenAsync(null, null);

        signedOut.Should().BeTrue();
        result.IsFailed.Should().BeTrue();
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcClient_Unreadable_Cache_Entry_Should_Log_The_User_In()
    {
        var oidc = CreateOidcClient(null, null);
        oidc.SecureCache.Setup(c => c.GetAsync<TokenRefreshInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new System.Text.Json.JsonException("The entry cannot be deserialized."));
        oidc.Browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.UserCancel });

        var exception = await Record.ExceptionAsync(async () => await oidc.Provider.GetTokenAsync(null, null));

        exception.Should().BeNull();
        oidc.Browser.Verify(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyErrorLogged(oidc.Logger);
        // The entry is removed, so the next calls do not read and log it again.
        oidc.SecureCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // The mock is not an Arc4u logger: the exception is logged as an error of the plain ILogger.
    private static void VerifyErrorLogged(Mock<ILogger<OidcClientIdentityModelTokenProvider>> logger)
        => logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);

    private sealed record OidcClientSetup(OidcClientIdentityModelTokenProvider Provider, TokenRefreshInfo Tokens, Mock<IBrowser> Browser, Mock<ISecureCache> SecureCache, GatedTokenEndpoint TokenEndpoint, Mock<ILogger<OidcClientIdentityModelTokenProvider>> Logger);

    // No network: the provider information is given and the token endpoint and the browser are fakes.
    private static OidcClientSetup CreateOidcClient(TokenInfo? accessToken, TokenInfo? refreshToken, TimeSpan? loginTimeout = null, TimeSpan? secureCacheTimeout = null, ILogger<OidcClientIdentityModelTokenProvider>? logger = null, Action? onExtraContextRead = null)
    {
        var loggerMock = new Mock<ILogger<OidcClientIdentityModelTokenProvider>>();
        var tokenEndpoint = new GatedTokenEndpoint();
        var browser = new Mock<IBrowser>();
        browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.Success, Response = "myapp://callback" });

        var oidcOptions = new OidcClientOptions
        {
            Authority = "https://login.example.com",
            ClientId = "client",
            Scope = "openid offline_access",
            RedirectUri = "myapp://callback",
            PostLogoutRedirectUri = "myapp://callback",
            Browser = browser.Object,
            BackchannelHandler = tokenEndpoint,
            ProviderInformation = new ProviderInformation
            {
                IssuerName = "https://login.example.com",
                AuthorizeEndpoint = "https://login.example.com/authorize",
                TokenEndpoint = "https://login.example.com/token",
                EndSessionEndpoint = "https://login.example.com/logout",
                KeySet = new Duende.IdentityModel.Jwk.JsonWebKeySet()
            }
        };

        var tokens = new TokenRefreshInfo { AccessToken = accessToken, RefreshToken = refreshToken };
        var extraContext = new ApiExtraContextAuthenticationOption { AuthorizationParameters = new SimpleKeyValueSettings(), TokenParameters = new SimpleKeyValueSettings() };
        var authenticationOptions = new OidcClientAuthenticationOptions
        {
            ForceRefreshTimeoutTimeSpan = TimeSpan.FromMinutes(5),
            RefreshTokenLifetime = TimeSpan.FromDays(1),
            LoginTimeout = loginTimeout ?? TimeSpan.FromMinutes(5),
            SecureCacheTimeout = secureCacheTimeout ?? TimeSpan.FromSeconds(30)
        };
        var secureCache = new Mock<ISecureCache>();

        var provider = new OidcClientIdentityModelTokenProvider(oidcOptions,
                                                                tokens,
                                                                secureCache.Object,
                                                                ExtraContextMonitor(extraContext, onExtraContextRead),
                                                                Mock.Of<IOptionsMonitor<OidcClientAuthenticationOptions>>(m => m.CurrentValue == authenticationOptions),
                                                                logger ?? loggerMock.Object);

        return new OidcClientSetup(provider, tokens, browser, secureCache, tokenEndpoint, loggerMock);
    }

    // The monitor of the extra parameters; onRead runs each time they are read (when a refresh or a login request is prepared).
    private static IOptionsMonitor<ApiExtraContextAuthenticationOption> ExtraContextMonitor(ApiExtraContextAuthenticationOption extraContext, Action? onRead)
    {
        var monitor = new Mock<IOptionsMonitor<ApiExtraContextAuthenticationOption>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(() =>
        {
            onRead?.Invoke();
            return extraContext;
        });
        return monitor.Object;
    }

    private sealed class GatedTokenEndpoint : HttpMessageHandler
    {
        public TaskCompletionSource Gate { get; } = new();

        public int Calls { get; private set; }

        public int ExpiresIn { get; set; } = 3600;

        // When set, the token endpoint answers this OAuth error (400).
        public string? Error { get; set; }

        // When set, the request fails (network error).
        public Exception? Failure { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            await Gate.Task.ConfigureAwait(false);
            if (Failure is not null)
            {
                throw Failure;
            }

            if (Error is not null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($$"""{"error":"{{Error}}"}""", Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"access_token":"new-access","refresh_token":"new-refresh","token_type":"Bearer","expires_in":{{ExpiresIn}}}""", Encoding.UTF8, "application/json")
            };
        }
    }
}
