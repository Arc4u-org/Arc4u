using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;

namespace Arc4u.OAuth2.TokenProviders
{
    /// <summary>
    /// An <see cref="ITokenProvider"/> that exchanges the access token of the current user for a token for another API, with the on-behalf-of flow
    /// (<c>urn:ietf:params:oauth:grant-type:jwt-bearer</c>). The user token is the access token of the scoped <see cref="TokenRefreshInfo"/> (the OpenID Connect session) or, if empty, the <see cref="ClaimsIdentity.BootstrapContext"/> of the current principal (the bearer token of the request).
    /// The <c>ClientId</c>, <c>ClientSecret</c> and <c>Scope</c> come from the settings (see <see cref="OnBehalfOfSettingsOptions"/>) and the request goes to the token endpoint of the <c>Default</c> authority.
    /// The resulting token is cached until it expires.
    /// </summary>
    /// <param name="tokenRefreshInfo">The scoped access and refresh tokens of the user.</param>
    /// <param name="cacheHelper">The helper giving access to the token cache.</param>
    /// <param name="activitySourceFactory">The factory of the activity source used for telemetry.</param>
    /// <param name="applicationContext">The application context giving the current principal.</param>
    /// <param name="authorities">The named authority options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="userObjectIdentifier">
    /// When registered, identifies the user of the index of the cached tokens from the claims configured in <see cref="ClaimsIdentifierOption"/>, as the claims cache does;
    /// otherwise the user is identified by the <c>oid</c> (or <c>sub</c>) claim of the token.
    /// </param>
    [Export(ProviderName, typeof(ITokenProvider))]
    public class AzureADOboTokenProvider(TokenRefreshInfo tokenRefreshInfo,
        ICacheHelper cacheHelper,
        IActivitySourceFactory activitySourceFactory,
        IApplicationContext applicationContext,
        IOptionsMonitor<AuthorityOptions> authorities,
        ILogger<AzureADOboTokenProvider> logger,
        IUserObjectIdentifier? userObjectIdentifier = null) : ISignOutTokenProvider
    {
        /// <summary>The key (<c>Obo</c>) under which the provider is registered.</summary>
        public const string ProviderName = "Obo";
        private readonly ActivitySource? _activitySource = activitySourceFactory?.GetArc4u();
        private readonly AuthorityOptions _defaultAuthority = authorities.Get("Default");

        /// <summary>Gets a token for the API designated by the settings, on behalf of the current user.</summary>
        /// <param name="settings">The on-behalf-of settings (client id, client secret and scope).</param>
        /// <param name="_">Not used.</param>
        /// <returns>The token, or a failed result when there is no principal or no token for the user.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpRequestException">The token endpoint does not answer with a success status code.</exception>
        public async ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? _)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (applicationContext.Principal is null)
            {
                return new Error("No principal exists.");
            }
            using var activity = _activitySource?.StartActivity("Get on behal of token", ActivityKind.Producer);

            var identity = applicationContext.Principal.Identity as ClaimsIdentity;

            var currentToken = GetCurrentToken(identity);

            if (currentToken is null)
            {
                // this should never happend!
                if (identity is null)
                {
                    return new Error("The identity given is null.");
                }

                return new Error($"Token cannot be retrieved for the AuthenticationType: {identity.AuthenticationType}");
            }

            var cache = cacheHelper.GetCache();

            var cacheKey = BuildCacheKey(settings, currentToken);

            var tokenFromCache = await cache.GetAsync<TokenInfo>(cacheKey).ConfigureAwait(false);

            // if the token is expired => we need to refresh it! 
            // Not all caches have a TTl defined for a specific key.
            if (tokenFromCache is not null)
            {
                JwtSecurityToken token = new(tokenFromCache.Token);

                // arbitrary 1 minute to have time to perform a request => must be a variable.
                if (token.ValidTo.Subtract(DateTime.UtcNow).TotalSeconds > 60)
                {
                    // Entries cached by previous versions have the type access_token: the token must be sent as a Bearer.
                    return new TokenInfo("Bearer", tokenFromCache.Token, tokenFromCache.ExpiresOnUtc);
                }
            }

            // We consider that the access token is still valid.
            // In a Obo from OpenIdConnect this is always the case.
            var pairs = new Dictionary<string, string>()
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "client_id", settings.Values[TokenKeys.ClientIdKey] },
                { "client_secret", settings.Values[TokenKeys.ClientSecret] },
                { "assertion", currentToken },
                { "scope", settings.Values[TokenKeys.Scope] },
                { "requested_token_use", "on_behalf_of" }
            };
            using var handler = new HttpClientHandler { UseDefaultCredentials = true };
            using var client = new HttpClient(handler);
            var content = new FormUrlEncodedContent(pairs);
            var endPoint = await _defaultAuthority.GetEndpointAsync(CancellationToken.None).ConfigureAwait(false);
            using var tokenResponse = await client.PostAsync(endPoint, content, CancellationToken.None).ConfigureAwait(false);
            {
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    if (IdentityModelEventSource.ShowPII)
                    {
                        logger.Technical().LogOboFailedReasonWithPII(tokenResponse.ReasonPhrase ?? "No reason found!");
                    }
                    else
                    {
                        logger.Technical().LogOboFailedWithNoReasonNoPII();
                    }
                }

                // throws an exception is not 200OK.
                tokenResponse.EnsureSuccessStatusCode();

                TokenInfo? oboToken = null;

                if (tokenResponse.IsSuccessStatusCode)
                {
                    using var payload = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false));

                    // Persist the new acess token
                    if (payload.RootElement.TryGetProperty("expires_in", out var property) && property.TryGetInt32(out var seconds))
                    {
                        var expirationAt = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
                        oboToken = new TokenInfo("Bearer", payload!.RootElement!.GetString("access_token") ?? string.Empty, expirationAt.ToUniversalTime());
                    }
                    else
                    {
                        oboToken = new TokenInfo("Bearer", payload!.RootElement!.GetString("access_token") ?? string.Empty);
                    }

                    await cache.PutAsync(cacheKey, oboToken.ExpiresOnUtc - DateTime.UtcNow, oboToken).ConfigureAwait(false);
                    await AddToIndexAsync(cache, currentToken, cacheKey, oboToken.ExpiresOnUtc, identity).ConfigureAwait(false);
                }

                if (oboToken is null)
                {
                    logger.Technical().LogError("No token was in the paylod of the message during the Obo request.");
                    return new Error("No token was in the paylod of the message during the Obo request.");
                }

                return oboToken.ToResult();
            }
        }

        /// <summary>
        /// Removes from the cache all the on-behalf-of tokens obtained for the current user, whatever the settings (API) and the user access token (before or after a refresh)
        /// they were obtained with: the keys are read from the <see cref="OboTokenCacheIndex"/> of the user. Nothing is done when there is no token for the user.
        /// A key that cannot be removed is logged and kept in the index, so a next sign-out of the user removes it.
        /// </summary>
        /// <param name="settings">Not used: the tokens of every on-behalf-of settings are removed.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the tokens are removed from the cache.</returns>
        public async ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
        {
            var identity = applicationContext.Principal?.Identity as ClaimsIdentity;
            var currentToken = GetCurrentToken(identity);

            if (currentToken is null)
            {
                return;
            }

            var cache = cacheHelper.GetCache();
            var indexKey = BuildIndexKey(currentToken, identity);

            var index = await cache.GetAsync<OboTokenCacheIndex>(indexKey, cancellationToken).ConfigureAwait(false);
            if (index is null)
            {
                return;
            }

            // The removals are independent: they run together, and a failure does not stop the others.
            var keys = index.Keys.Keys.ToList();
            var removed = await Task.WhenAll(keys.Select(async key =>
            {
                try
                {
                    await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogTechnicalException(ex, "An on-behalf-of token cannot be removed from the cache at the sign-out.");
                    return false;
                }
            })).ConfigureAwait(false);

            if (removed.All(r => r))
            {
                await cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);
                return;
            }

            var remaining = new OboTokenCacheIndex(keys.Where((_, i) => !removed[i]).ToDictionary(key => key, key => index.Keys[key]));
            var lifetime = remaining.ExpiresOnUtc - DateTime.UtcNow;
            if (lifetime > TimeSpan.Zero)
            {
                await cache.PutAsync(indexKey, lifetime, remaining, cancellation: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);
            }
        }

        // Records the key of a cached token in the index of the user, so the sign-out finds the tokens of every API without their settings.
        // The expired keys are dropped, and the index is written only when it changes.
        // The read-modify-write is not atomic: two concurrent requests for different APIs can lose a key; that token then expires on its own.
        internal async Task AddToIndexAsync(ICache cache, string currentToken, string cacheKey, DateTime expiresOnUtc, ClaimsIdentity? identity = null)
        {
            try
            {
                var indexKey = BuildIndexKey(currentToken, identity);
                var index = await cache.GetAsync<OboTokenCacheIndex>(indexKey).ConfigureAwait(false);

                if (index is not null && index.Keys.TryGetValue(cacheKey, out var known) && known >= expiresOnUtc)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var keys = (index?.Keys ?? []).Where(entry => entry.Value > now).ToDictionary(entry => entry.Key, entry => entry.Value);
                keys[cacheKey] = expiresOnUtc;

                var indexEntry = new OboTokenCacheIndex(keys);
                if (indexEntry.ExpiresOnUtc > now)
                {
                    await cache.PutAsync(indexKey, indexEntry.ExpiresOnUtc - now, indexEntry).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // The token is cached and returned anyway: only its removal at sign-out is lost.
                logger.LogTechnicalException(ex, "The key of an on-behalf-of token cannot be added to the index of the user.");
            }
        }

        // The index is kept per user and per application, not per user access token, so the tokens obtained before a refresh of the access token are found too,
        // and two applications sharing the cache do not share an index.
        // - The application is the aud claim of the user token: the on-behalf-of flow requires it to be the application doing the exchange (azp or appid,
        //   the client that requested the user token, are only used when there is no audience).
        // - The user is identified from the claims of the user token, so the key is the same whether the principal is available or not (sign-out without principal):
        //   by the IUserObjectIdentifier when one is registered (the claims configured for the application), otherwise by the oid (or sub) claim.
        //   A token that is not a JWT has no claims: the identity of the principal is used, or the token identifies itself.
        private string BuildIndexKey(string currentToken, ClaimsIdentity? identity)
            => $"_Obo_Index_{TokenCacheKey.Hash(_defaultAuthority.Url.ToString(), GetUserIdentifier(currentToken, identity))}";

        private string GetUserIdentifier(string currentToken, ClaimsIdentity? identity)
        {
            var jwt = ReadJwt(currentToken);
            if (jwt is null)
            {
                return (identity is null ? null : Identify(identity)) ?? currentToken;
            }

            // The identity is only built when an identifier is registered to read it.
            var user = (userObjectIdentifier is null ? null : Identify(CreateIdentity(jwt)))
                       ?? GetClaim(jwt, "oid")
                       ?? (jwt.Subject is { Length: > 0 } subject ? subject : null);
            if (user is null)
            {
                return currentToken;
            }

            var application = jwt.Audiences.Any() ? string.Join(' ', jwt.Audiences) : GetClaim(jwt, "azp") ?? GetClaim(jwt, "appid");
            return $"{application}|{user}";
        }

        private static JwtSecurityToken? ReadJwt(string token)
        {
            try
            {
                return new JwtSecurityToken(token);
            }
            catch (Exception)
            {
                // Not a JWT (an opaque token).
                return null;
            }
        }

        // The claims of the token with their raw type (oid, upn...) and the type the JWT handlers map them to, so the claim types configured for the
        // IUserObjectIdentifier match the token as they match the principal.
        private static ClaimsIdentity CreateIdentity(JwtSecurityToken jwt)
        {
            var claims = new List<Claim>();
            foreach (var claim in jwt.Claims)
            {
                claims.Add(new Claim(claim.Type, claim.Value));
                if (JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out var mappedType) && mappedType != claim.Type)
                {
                    claims.Add(new Claim(mappedType, claim.Value));
                }
            }

            return new ClaimsIdentity(claims, "Bearer");
        }

        private string? Identify(ClaimsIdentity identity)
        {
            if (userObjectIdentifier is null)
            {
                return null;
            }

            try
            {
                return userObjectIdentifier.Getidentifier(identity) is { Length: > 0 } id ? id : null;
            }
            catch (Exception ex)
            {
                logger.LogTechnicalException(ex, "The registered IUserObjectIdentifier cannot identify the user of the token.");
                return null;
            }
        }

        private static string? GetClaim(JwtSecurityToken jwt, string type)
            => jwt.Payload.TryGetValue(type, out var value) && value?.ToString() is { Length: > 0 } claim ? claim : null;

        // The user token is, with OpenID Connect, the one of the cookie or, otherwise, the one of the incoming request.
        // The cookie token is read first: the OpenIdBearerInjectorMiddleware replaces the BootstrapContext with the on-behalf-of token it obtained,
        // which is not a token of the user for this application (wrong audience for another exchange, and another index at sign-out).
        private string? GetCurrentToken(ClaimsIdentity? identity)
            => tokenRefreshInfo?.AccessToken?.Token is { Length: > 0 } cookieToken ? cookieToken : identity?.BootstrapContext?.ToString();

        // the key is defined by user!
        // The hash must be stable across processes (the cache can be distributed): string.GetHashCode is not.
        private string BuildCacheKey(IKeyValueSettings settings, string currentToken)
            => $"_Obo_{settings.Values[TokenKeys.ClientIdKey]}_{TokenCacheKey.Hash(_defaultAuthority.Url.ToString(), currentToken, settings.Values[TokenKeys.Scope])}";
    }
}
