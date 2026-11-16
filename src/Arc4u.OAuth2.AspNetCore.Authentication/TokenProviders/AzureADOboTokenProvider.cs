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
    /// <param name="httpClientFactory">
    /// When registered, creates the <see cref="HttpClient"/> that calls the token endpoint: the one named by the <c>HttpClientName</c> setting, or the default one.
    /// Otherwise a shared <see cref="HttpClient"/> is used.
    /// </param>
    /// <param name="tokenCacheResolver">
    /// Resolves the cache of the token cache options. The <c>TokenCacheOptionsName</c> setting selects named options (see <see cref="OnBehalfOfSettingsOptions.TokenCache"/>): the tokens are stored in their cache,
    /// for at most their <see cref="TokenCacheOptions.MaxTime"/>. When <see langword="null"/>, the tokens are stored in the token cache of the <see cref="ICacheHelper"/> until they expire.
    /// The index of the tokens of a user is always stored in the token cache of the <see cref="ICacheHelper"/>.
    /// </param>
    [Export(ProviderName, typeof(ITokenProvider))]
    public class AzureADOboTokenProvider(TokenRefreshInfo tokenRefreshInfo,
        ICacheHelper cacheHelper,
        IActivitySourceFactory activitySourceFactory,
        IApplicationContext applicationContext,
        IOptionsMonitor<AuthorityOptions> authorities,
        ILogger<AzureADOboTokenProvider> logger,
        IUserObjectIdentifier? userObjectIdentifier = null,
        IHttpClientFactory? httpClientFactory = null,
        TokenCacheResolver? tokenCacheResolver = null) : ISignOutTokenProvider
    {
        /// <summary>The key (<c>Obo</c>) under which the provider is registered.</summary>
        public const string ProviderName = "Obo";

        // Used when no IHttpClientFactory is registered: one client for the process, so the connections are pooled (a client per call exhausts the sockets),
        // with a limited connection lifetime so DNS changes of the token endpoint are seen.
        private static readonly HttpClient SharedHttpClient = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

        // A cached token is used only when it is valid for at least this time, to leave time to perform the request.
        private static readonly TimeSpan ExpirationMargin = TimeSpan.FromMinutes(1);

        // The longest lifetime read from expires_in: a larger value is not a real lifetime.
        private static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(365);

        private readonly ActivitySource? _activitySource = activitySourceFactory?.GetArc4u();
        private readonly AuthorityOptions _defaultAuthority = authorities.Get("Default");

        /// <summary>Gets a token for the API designated by the settings, on behalf of the current user.</summary>
        /// <param name="settings">The on-behalf-of settings (client id, client secret and scope).</param>
        /// <param name="platformParameters">Not used.</param>
        /// <returns>
        /// The token, or a failed result when there is no principal or no token for the user, a setting is missing, the token endpoint does not answer with a success
        /// status code, or its response has no access token.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        public async ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var validation = new Result();
            var clientId = Require(settings, TokenKeys.ClientIdKey, validation);
            var clientSecret = Require(settings, TokenKeys.ClientSecret, validation);
            var scope = Require(settings, TokenKeys.Scope, validation);
            if (validation.IsFailed)
            {
                return validation;
            }

            if (applicationContext.Principal is null)
            {
                return new Error("No principal exists.");
            }
            using var activity = _activitySource?.StartActivity("Get on behalf of token", ActivityKind.Producer);

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

            // The token is in the cache of its TokenCacheOptions; the index of the user is always in the token cache, so the sign-out finds it.
            settings.Values.TryGetValue(TokenKeys.TokenCacheOptionsName, out var optionsName);
            optionsName = string.IsNullOrWhiteSpace(optionsName) ? null : optionsName;
            var (cache, maxTime, cacheName) = ResolveTokenCache(optionsName);

            var cacheKey = BuildCacheKey(clientId!, scope!, currentToken, cacheName);

            var tokenFromCache = await cache.GetAsync<TokenInfo>(cacheKey).ConfigureAwait(false);

            // The cache may not evict the entry exactly at the expiration: the expiration of the token is checked too.
            // The expiration is read from the TokenInfo, so a token that is not a JWT (an opaque token) is served from the cache as well.
            if (tokenFromCache is not null && !string.IsNullOrWhiteSpace(tokenFromCache.Token) && tokenFromCache.ExpiresOnUtc - DateTime.UtcNow > ExpirationMargin)
            {
                // Entries cached by previous versions have the type access_token: the token must be sent as a Bearer.
                return new TokenInfo("Bearer", tokenFromCache.Token, tokenFromCache.ExpiresOnUtc);
            }

            // We consider that the access token is still valid.
            // In a Obo from OpenIdConnect this is always the case.
            var pairs = new Dictionary<string, string>()
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "client_id", clientId! },
                { "client_secret", clientSecret! },
                { "assertion", currentToken },
                { "scope", scope! },
                { "requested_token_use", "on_behalf_of" }
            };

            var result = await RequestTokenAsync(settings, pairs).ConfigureAwait(false);
            if (result.IsFailed)
            {
                return result;
            }

            var oboToken = result.Value;

            // A token without lifetime (expires_in of 0, or no expiration known) is returned but not cached: some caches reject a timeout that is not positive.
            // Nor is a token valid for at most the ExpirationMargin: it would never be read from the cache.
            // The token is kept until it expires, and at most the MaxTime of its options.
            // The margin is compared with the lifetime of the token, not with the MaxTime: a short MaxTime only shortens the time in the cache.
            var tokenLifetime = oboToken.ExpiresOnUtc - DateTime.UtcNow;
            var lifetime = tokenLifetime > maxTime ? maxTime : tokenLifetime;

            if (tokenLifetime > ExpirationMargin && lifetime > TimeSpan.Zero)
            {
                await cache.PutAsync(cacheKey, lifetime, oboToken).ConfigureAwait(false);
                await AddToIndexAsync(cacheHelper.GetCache(), currentToken, cacheKey, DateTime.UtcNow + lifetime, identity, cacheName).ConfigureAwait(false);
            }

            return oboToken;
        }

        // Without the token cache options, the tokens are in the token cache until they expire.
        private ResolvedTokenCache ResolveTokenCache(string? optionsName)
            => tokenCacheResolver?.Resolve(optionsName) ?? new ResolvedTokenCache(cacheHelper.GetCache(), TimeSpan.MaxValue);

        private static string? Require(IKeyValueSettings settings, string key, Result validation)
        {
            if (settings.Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            validation.WithError($"{key} is missing. Cannot process the on-behalf-of request.");
            return null;
        }

        private HttpClient GetHttpClient(IKeyValueSettings settings)
        {
            if (httpClientFactory is null)
            {
                return SharedHttpClient;
            }

            return settings.Values.TryGetValue(TokenKeys.HttpClientName, out var name) && !string.IsNullOrWhiteSpace(name)
                ? httpClientFactory.CreateClient(name)
                : httpClientFactory.CreateClient();
        }

        private async Task<Result<TokenInfo>> RequestTokenAsync(IKeyValueSettings settings, Dictionary<string, string> pairs)
        {
            var endPoint = await _defaultAuthority.GetEndpointAsync(CancellationToken.None).ConfigureAwait(false);

            using var content = new FormUrlEncodedContent(pairs);
            using var tokenResponse = await GetHttpClient(settings).PostAsync(endPoint, content, CancellationToken.None).ConfigureAwait(false);

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

                return new Error($"The on-behalf-of token request failed with the status code {(int)tokenResponse.StatusCode}.");
            }

            var body = await tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            JsonDocument payload;
            try
            {
                payload = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                logger.Technical().LogError("The response of the on-behalf-of request is not Json.");
                return new Error("The response of the on-behalf-of request is not Json.");
            }

            using (payload)
            {
                var root = payload.RootElement;

                var accessToken = root.ValueKind == JsonValueKind.Object
                                  && root.TryGetProperty("access_token", out var accessTokenProperty)
                                  && accessTokenProperty.ValueKind == JsonValueKind.String
                    ? accessTokenProperty.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    logger.Technical().LogError("No token was in the payload of the message during the Obo request.");
                    return new Error("No token was in the payload of the message during the Obo request.");
                }

                return new TokenInfo("Bearer", accessToken, GetExpiration(root, accessToken));
            }
        }

        // A negative expires_in expires now; an unreasonable one (beyond MaxLifetime) is limited, so DateTime.AddSeconds cannot overflow.
        private static DateTime ExpiresIn(long seconds)
            => DateTime.UtcNow.AddSeconds(Math.Clamp(seconds, 0, (long)MaxLifetime.TotalSeconds));

        // expires_in is a number, or a string with some identity providers (the Azure AD v1 endpoint).
        // Without it, the expiration of a JWT is used; an opaque token without expires_in has no known expiration: it expires now, so it is not cached.
        private DateTime GetExpiration(JsonElement root, string accessToken)
        {
            if (root.TryGetProperty("expires_in", out var property))
            {
                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var seconds))
                {
                    return ExpiresIn(seconds);
                }

                if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                {
                    return ExpiresIn(seconds);
                }
            }

            if (ReadJwt(accessToken) is { } jwt)
            {
                return jwt.ValidTo.ToUniversalTime();
            }

            logger.Technical().LogWarning("The on-behalf-of token has no expires_in and is not a JWT: it is not cached.");
            return DateTime.UtcNow;
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
                    // Each token is removed from its cache, found by its name: the options of the token may have changed since it was cached.
                    var cacheName = index.GetCacheName(key);
                    var tokenCache = cacheName is null ? cache : cacheHelper.GetCache(cacheName);
                    await tokenCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
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

            var remainingKeys = keys.Where((_, i) => !removed[i]).ToList();
            var remaining = new OboTokenCacheIndex(remainingKeys.ToDictionary(key => key, key => index.Keys[key]),
                                                   KeepCacheNames(index.CacheNames, remainingKeys));
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
        internal async Task AddToIndexAsync(ICache cache, string currentToken, string cacheKey, DateTime expiresOnUtc, ClaimsIdentity? identity = null, string? cacheName = null)
        {
            try
            {
                var indexKey = BuildIndexKey(currentToken, identity);
                var index = await cache.GetAsync<OboTokenCacheIndex>(indexKey).ConfigureAwait(false);

                if (index is not null && index.Keys.TryGetValue(cacheKey, out var known) && known >= expiresOnUtc && index.GetCacheName(cacheKey) == cacheName)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var keys = (index?.Keys ?? []).Where(entry => entry.Value > now).ToDictionary(entry => entry.Key, entry => entry.Value);
                keys[cacheKey] = expiresOnUtc;

                var cacheNames = KeepCacheNames(index?.CacheNames, keys.Keys);
                if (cacheName is null)
                {
                    cacheNames?.Remove(cacheKey);
                }
                else
                {
                    (cacheNames ??= new Dictionary<string, string>(StringComparer.Ordinal))[cacheKey] = cacheName;
                }

                var indexEntry = new OboTokenCacheIndex(keys, cacheNames is { Count: > 0 } ? cacheNames : null);
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

        // The cache names of the given keys; null when there is none, so an index of tokens in the token cache keeps the format of the previous versions.
        private static Dictionary<string, string>? KeepCacheNames(Dictionary<string, string>? cacheNames, IEnumerable<string> keys)
        {
            if (cacheNames is null)
            {
                return null;
            }

            var kept = keys.Where(cacheNames.ContainsKey).ToDictionary(key => key, key => cacheNames[key], StringComparer.Ordinal);
            return kept.Count > 0 ? kept : null;
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
        // The name of a named cache is part of the key, so two settings with the same client and scope but different caches have their own entry in the index.
        // A token in the token cache keeps the key of the previous versions.
        private string BuildCacheKey(string clientId, string scope, string currentToken, string? cacheName)
            => cacheName is null
                ? $"_Obo_{clientId}_{TokenCacheKey.Hash(_defaultAuthority.Url.ToString(), currentToken, scope)}"
                : $"_Obo_{clientId}_{TokenCacheKey.Hash(_defaultAuthority.Url.ToString(), currentToken, scope, cacheName)}";
    }
}
