using System.Security.Cryptography;
using System.Text.Json;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Client.Authentication.Options;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Client.Authentication.TokenProvider;

/// <summary>
/// An <see cref="ITokenProvider"/> for a client application using <c>Duende.IdentityModel.OidcClient</c>. The tokens are kept in the secure cache; an expired access token is renewed with the refresh token,
/// and the user is logged in through the browser when there is no valid refresh token. The access token is refreshed when it expires in less than
/// <see cref="OidcClientAuthenticationOptions.ForceRefreshTimeoutTimeSpan"/>, and the refresh token is assumed valid for <see cref="OidcClientAuthenticationOptions.RefreshTokenLifetime"/>.
/// </summary>
/// <param name="options">The <c>OidcClient</c> options.</param>
/// <param name="tokensInfo">The access and refresh tokens of the user.</param>
/// <param name="secureCache">The secure cache in which the tokens are persisted.</param>
/// <param name="apiExtraContextOption">The extra parameters added to the authorization (front channel) and token (back channel) requests.</param>
/// <param name="authenticationOptions">The client authentication options, giving the refresh threshold and the refresh token lifetime.</param>
/// <param name="logger">The logger of the secure-cache failures. Optional: nothing is logged when no logger is registered.</param>
[Export(TokenProviderName, typeof(ITokenProvider)), Shared]
public class OidcClientIdentityModelTokenProvider(
                                                    OidcClientOptions options,
                                                    TokenRefreshInfo tokensInfo,
                                                    ISecureCache secureCache,
                                                    IOptionsMonitor<ApiExtraContextAuthenticationOption> apiExtraContextOption,
                                                    IOptionsMonitor<OidcClientAuthenticationOptions> authenticationOptions,
                                                    ILogger<OidcClientIdentityModelTokenProvider>? logger = null) : ISignOutTokenProvider
{
    private readonly ILogger<OidcClientIdentityModelTokenProvider> _logger = logger ?? NullLogger<OidcClientIdentityModelTokenProvider>.Instance;

    /// <summary>The key (<c>OidcClientIdentityModel</c>) under which the provider is registered.</summary>
    public const string TokenProviderName = "OidcClientIdentityModel";
    private const string TokenKey = "TokensInfo";
    private const string TokenType = "Bearer";

    // Incremented by each sign-out, so tokens obtained by a refresh or a login in progress during a sign-out are not kept after it.
    // The provider is [Shared] (a singleton, like its TokenRefreshInfo): the instance signed out is the one running GetTokenAsync.
    private readonly SignOutGeneration _signOutGeneration = new();

    // Serializes the writes of the tokens with the sign-out: a write checked before a sign-out cannot complete after it.
    private readonly SemaphoreSlim _tokensLock = new(1, 1);

    // One refresh or login at a time: concurrent calls would use the same refresh token, and with refresh token rotation the second refresh fails,
    // clears the tokens the first one stored and starts an interactive login. A call waiting for it uses the token obtained by the call before.
    // The wait and the login are bounded by OidcClientAuthenticationOptions.LoginTimeout, so a login the user never completes does not block the calls forever.
    private readonly SemaphoreSlim _acquireLock = new(1, 1);

    // Incremented (under _acquireLock) by each refresh or login attempt, with its result: a call that waited for an attempt returns its result
    // instead of starting another login (when the user cancels the login, every queued call would open the browser again).
    private int _acquireAttempts;
    private Result<TokenInfo>? _lastAttemptResult;

    // The cancellation of the login in progress: a sign-out cancels it, so its window does not stay open for tokens that would be discarded.
    private CancellationTokenSource? _loginCancellation;

    // FluentResults results are mutable: each caller gets its own copy of a result shared between the calls.
    private static Result<TokenInfo> Copy(Result<TokenInfo> result)
        => result.IsSuccess ? FluentResults.Result.Ok(result.Value) : new Result<TokenInfo>().WithErrors(result.Errors);

    private void CancelLoginInProgress()
    {
        try
        {
            Volatile.Read(ref _loginCancellation)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The login ended meanwhile.
        }
    }

    // Removes an entry that cannot be read, so every call does not read (and log) it again. A valid entry written meanwhile by a login (the tokens are in memory) is kept.
    private async Task RemoveUnreadableEntryAsync()
    {
        await _tokensLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (tokensInfo.AccessToken is null && tokensInfo.RefreshToken is null)
            {
                using var timeout = new CancellationTokenSource(authenticationOptions.CurrentValue.SecureCacheTimeout);
                await secureCache.RemoveAsync(TokenKey, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTechnicalException(ex, "The unreadable tokens cannot be removed from the secure cache.");
        }
        finally
        {
            _tokensLock.Release();
        }
    }

    // The entry exists but cannot be decoded (format change, corrupted or undecryptable file): unlike a transient failure (file locked, timeout),
    // reading it again cannot succeed, so it is removed.
    private static bool IsUnreadable(Exception exception)
        => exception is JsonException or KeyNotFoundException or CryptographicException or FormatException;

    // The secure cache cannot be used, and will not be by retrying: access denied, missing folder, or a serializer configuration that does not support
    // TokenRefreshInfo (NotSupportedException). The entry is kept (it may be valid), but the user logs in so the application can work.
    private static bool IsInaccessible(Exception exception)
        => exception is UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException or NotSupportedException;

    // The OAuth errors meaning the refresh token cannot be used any more (revoked, expired, not granted for the requested scopes, or an interaction is required):
    // the user must log in again. Any other refresh error (network, server) is transient: the tokens are kept.
    private static bool IsRefreshTokenRejected(string? error)
        => error is "invalid_grant" or "invalid_scope" or "interaction_required" or "login_required" or "consent_required";

    private static bool IsValid(TokenInfo? accessToken, OidcClientAuthenticationOptions clientOptions)
        => accessToken is not null && accessToken.ExpiresOnUtc - clientOptions.ForceRefreshTimeoutTimeSpan > DateTime.UtcNow;

    // Stores the tokens, unless the user signed out since signOutCount was read. Returns false when the tokens are not stored.
    // The write runs under _tokensLock and is bounded by SecureCacheTimeout, so a stuck cache cannot block the sign-out and the other writes forever.
    // The identity token replaces the current one when it is given; null keeps it (a refresh response does not always contain one).
    private async Task<bool> TryStoreTokensAsync(int signOutCount, TokenInfo? accessToken, TokenInfo? refreshToken, string? identityToken, bool persist)
    {
        await _tokensLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_signOutGeneration.HasChanged(signOutCount))
            {
                return false;
            }

            tokensInfo.AccessToken = accessToken;
            tokensInfo.RefreshToken = refreshToken;
            if (identityToken is not null)
            {
                tokensInfo.IdentityToken = identityToken;
            }

            if (persist)
            {
                using var timeout = new CancellationTokenSource(authenticationOptions.CurrentValue.SecureCacheTimeout);
                try
                {
                    await secureCache.PutAsync(TokenKey, tokensInfo, timeout.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // The secure cache failed or did not answer: the tokens are valid and kept in memory only, until the application stops.
                    _logger.LogTechnicalException(ex, "The tokens cannot be written to the secure cache: they are kept in memory only.");
                }
            }

            return true;
        }
        finally
        {
            _tokensLock.Release();
        }
    }

    /// <summary>Gets the access token of the user, refreshing it or logging the user in when necessary.</summary>
    /// <param name="settings">The provider settings. They are not used.</param>
    /// <param name="platformParameters">Not used.</param>
    /// <returns>The access token, or a failed result when the login failed. When the refresh of the token fails, the user is logged in again.</returns>
    public async ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        var signOutCount = _signOutGeneration.Snapshot();

        // Check if the token exists?
        if (null == tokensInfo.AccessToken)
        {
            TokenRefreshInfo tokens;
            try
            {
                tokens = await secureCache.GetAsync<TokenRefreshInfo>(TokenKey).ConfigureAwait(false) ?? new TokenRefreshInfo();
            }
            catch (Exception ex) when (IsUnreadable(ex))
            {
                // An entry that cannot be decoded is like no entry: it is removed and the user logs in again.
                _logger.LogTechnicalException(ex, "The tokens in the secure cache cannot be decoded: the user logs in again.");
                await RemoveUnreadableEntryAsync().ConfigureAwait(false);
                tokens = new TokenRefreshInfo();
            }
            catch (Exception ex) when (IsInaccessible(ex))
            {
                _logger.LogTechnicalException(ex, "The secure cache cannot be used (access denied, missing folder or serializer configuration): the user logs in again.");
                tokens = new TokenRefreshInfo();
            }
            catch (Exception ex)
            {
                // A transient failure (file locked, timeout or cancellation of the cache itself, since GetTokenAsync has no cancellation token):
                // the call fails, but the entry is kept and the user is not asked to log in.
                _logger.LogTechnicalException(ex, "The secure cache cannot be read: the tokens are kept for the next call.");
                return FluentResults.Result.Fail(new ExceptionalError("The secure cache cannot be read.", ex));
            }

            if (!await TryStoreTokensAsync(signOutCount, tokens.AccessToken, tokens.RefreshToken, tokens.IdentityToken, persist: false).ConfigureAwait(false))
            {
                return FluentResults.Result.Fail("The user signed out while the token was requested.");
            }
        }

        var clientOptions = authenticationOptions.CurrentValue;

        // The access token is renewed before it expires, so it does not expire while a call is in flight.
        // The token is read once: a sign-out on another thread can clear it between a check and a second read.
        var accessToken = tokensInfo.AccessToken;
        if (IsValid(accessToken, clientOptions))
        {
            return accessToken!;
        }

        var acquireAttempts = Volatile.Read(ref _acquireAttempts);
        if (!await _acquireLock.WaitAsync(clientOptions.LoginTimeout).ConfigureAwait(false))
        {
            return FluentResults.Result.Fail("The token is still being obtained by another call (a login in progress).");
        }

        try
        {
            if (_signOutGeneration.HasChanged(signOutCount))
            {
                return FluentResults.Result.Fail("The user signed out while the token was requested.");
            }

            // Another call refreshed the token (or logged the user in) while this one was waiting.
            accessToken = tokensInfo.AccessToken;
            if (IsValid(accessToken, clientOptions))
            {
                return accessToken!;
            }

            // Another call tried while this one was waiting: its result (the token, or why the login was cancelled or failed) is returned, without another attempt.
            if (_acquireAttempts != acquireAttempts && _lastAttemptResult is not null)
            {
                return Copy(_lastAttemptResult);
            }

            Result<TokenInfo> result = FluentResults.Result.Fail("The token could not be obtained.");
            try
            {
                result = await AcquireTokenAsync(signOutCount, clientOptions).ConfigureAwait(false);
                return result;
            }
            finally
            {
                _lastAttemptResult = Copy(result);
                _acquireAttempts++;
            }
        }
        finally
        {
            _acquireLock.Release();
        }
    }

    // Refreshes the token or, when it cannot be refreshed, logs the user in. It runs under _acquireLock.
    private async Task<Result<TokenInfo>> AcquireTokenAsync(int signOutCount, OidcClientAuthenticationOptions clientOptions)
    {
        var client = new OidcClient(options);

        // The refresh token is read once: a sign-out on another thread can clear it between a check and a second read.
        var refreshToken = tokensInfo.RefreshToken;
        if (refreshToken is not null && refreshToken.ExpiresOnUtc > DateTime.UtcNow)
        {
            // The refresh is a token endpoint (back channel) request: only the token parameters apply.
            var parameters = new Parameters();
            foreach (var parameter in apiExtraContextOption.CurrentValue.TokenParameters.Values)
            {
                parameters.Add(parameter.Key, parameter.Value);
            }

            var result = await client.RefreshTokenAsync(refreshToken.Token, parameters).ConfigureAwait(false);

            if (_signOutGeneration.HasChanged(signOutCount))
            {
                return FluentResults.Result.Fail("The user signed out while the token was refreshed.");
            }

            if (!result.IsError)
            {
                var refreshedToken = new TokenInfo(TokenType, result.AccessToken, result.AccessTokenExpiration.UtcDateTime);
                if (!await TryStoreTokensAsync(signOutCount, refreshedToken, new TokenInfo(TokenType, result.RefreshToken, DateTime.UtcNow + clientOptions.RefreshTokenLifetime), result.IdentityToken, persist: true).ConfigureAwait(false))
                {
                    return FluentResults.Result.Fail("The user signed out while the token was refreshed.");
                }

                return refreshedToken;
            }

            if (!IsRefreshTokenRejected(result.Error))
            {
                // A network or server failure: the refresh token is still valid, so the tokens are kept and the user is not asked to log in.
                // The access token is renewed before it expires: while it has not expired, it is still returned.
                var currentToken = tokensInfo.AccessToken;
                if (currentToken is not null && currentToken.ExpiresOnUtc > DateTime.UtcNow)
                {
                    return currentToken;
                }

                return FluentResults.Result.Fail($"The token cannot be refreshed: {result.Error} {result.ErrorDescription}".TrimEnd());
            }

            // The refresh token is rejected (revoked or expired at the authority): the user must log in again.
            tokensInfo.RefreshToken = null;
            tokensInfo.AccessToken = null;
        }

        // Perform the login: the authorization parameters go to the authorize request (front channel, in the browser),
        // the token parameters to the code exchange at the token endpoint (back channel).
        var loginRequest = new LoginRequest();
        foreach (var parameter in apiExtraContextOption.CurrentValue.AuthorizationParameters.Values)
        {
            loginRequest.FrontChannelExtraParameters.Add(parameter.Key, parameter.Value);
        }
        foreach (var parameter in apiExtraContextOption.CurrentValue.TokenParameters.Values)
        {
            loginRequest.BackChannelExtraParameters.Add(parameter.Key, parameter.Value);
        }
        LoginResult loginResult;
        using (var loginCancellation = new CancellationTokenSource(clientOptions.LoginTimeout))
        {
            // The cancellation is published (full fence) before the sign-out is checked, and a sign-out increments the generation before it cancels the login:
            // a sign-out either cancels this login or is seen by the check, so the browser never opens for a session that was just ended.
            Interlocked.Exchange(ref _loginCancellation, loginCancellation);
            try
            {
                if (_signOutGeneration.HasChanged(signOutCount))
                {
                    return FluentResults.Result.Fail("The user signed out while logging in.");
                }

                loginResult = await client.LoginAsync(loginRequest, loginCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (loginCancellation.IsCancellationRequested)
            {
                // Cancelled by a sign-out, or not completed in time.
                return _signOutGeneration.HasChanged(signOutCount)
                    ? FluentResults.Result.Fail("The user signed out while logging in.")
                    : FluentResults.Result.Fail($"The login did not complete within {clientOptions.LoginTimeout}.");
            }
            finally
            {
                Interlocked.Exchange(ref _loginCancellation, null);
            }
        }

        if (_signOutGeneration.HasChanged(signOutCount))
        {
            return FluentResults.Result.Fail("The user signed out while logging in.");
        }

        if (loginResult.IsError)
        {
            return FluentResults.Result.Fail($"The login failed: {loginResult.Error} {loginResult.ErrorDescription}".TrimEnd());
        }

        var loggedInToken = new TokenInfo(TokenType, loginResult.AccessToken, loginResult.AccessTokenExpiration.UtcDateTime);
        if (!await TryStoreTokensAsync(signOutCount, loggedInToken, new TokenInfo(TokenType, loginResult.RefreshToken, DateTime.UtcNow + clientOptions.RefreshTokenLifetime), loginResult.IdentityToken, persist: true).ConfigureAwait(false))
        {
            return FluentResults.Result.Fail("The user signed out while logging in.");
        }

        return loggedInToken;
    }

    /// <summary>
    /// Cancels a login in progress, removes the tokens from memory and from the secure cache, then logs the user out of the authority.
    /// The logout (in the browser) is skipped when no tokens existed: the user did not sign in with this provider. It is bounded by <see cref="OidcClientAuthenticationOptions.LoginTimeout"/>.
    /// A failure of the secure cache or of the logout is logged: the other steps are done anyway.
    /// </summary>
    /// <param name="settings">The provider settings, which are not used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is logged out.</returns>
    public async ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        bool hadSession;
        string? idTokenHint;

        // The tokens are cleared under the lock of TryStoreTokensAsync: a write checked before the sign-out completes before it, and is then removed.
        await _tokensLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(authenticationOptions.CurrentValue.SecureCacheTimeout);

            _signOutGeneration.Increment();

            hadSession = tokensInfo.AccessToken is not null || tokensInfo.RefreshToken is not null;
            idTokenHint = tokensInfo.IdentityToken;

            // The tokens are kept in the (singleton) TokenRefreshInfo: without this, GetTokenAsync keeps returning the access token after the sign-out.
            tokensInfo.AccessToken = null;
            tokensInfo.RefreshToken = null;
            tokensInfo.IdentityToken = null;

            if (!hadSession)
            {
                try
                {
                    var cached = await secureCache.GetAsync<TokenRefreshInfo>(TokenKey, timeout.Token).ConfigureAwait(false);
                    hadSession = cached is not null;
                    idTokenHint ??= cached?.IdentityToken;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // An entry that cannot be read (format change, corrupted file, timeout) exists: it is removed below and the user is logged out.
                    _logger.LogTechnicalException(ex, "The tokens cannot be read from the secure cache during the sign-out: they are removed.");
                    hadSession = true;
                }
            }

            try
            {
                await secureCache.RemoveAsync(TokenKey, timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The entry stays in the secure cache (a next login replaces it), but the user is still logged out of the authority.
                _logger.LogTechnicalException(ex, "The tokens cannot be removed from the secure cache.");
                hadSession = true;
            }
        }
        finally
        {
            _tokensLock.Release();
        }

        // A login in progress is cancelled once the sign-out is recorded: it then reports the sign-out.
        CancelLoginInProgress();

        if (!hadSession)
        {
            return;
        }

        // The logout is in the browser (interactive): it runs outside the lock, and it is bounded so the sign-out of the other providers does not wait forever.
        using var logoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        logoutCancellation.CancelAfter(authenticationOptions.CurrentValue.LoginTimeout);
        try
        {
            // The identity token of the session tells the authority which session to end (no account picker).
            await new OidcClient(options).LogoutAsync(new LogoutRequest { IdTokenHint = idTokenHint }, logoutCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogTechnicalException(ex, "The logout at the authority did not complete in time.");
        }
    }
}
