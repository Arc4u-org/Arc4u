using System.Text.Json.Serialization;

namespace Arc4u.OAuth2.TokenProviders
{
    /// <summary>
    /// The cache keys of the on-behalf-of tokens obtained for one user, with their expiration, stored by <see cref="AzureADOboTokenProvider"/> so they can be removed when the user signs out.
    /// When the cache serializer uses a <c>JsonSerializerContext</c>, add this type to it.
    /// </summary>
    /// <param name="Keys">The cache keys of the tokens and their expiration date (UTC).</param>
    /// <param name="CacheNames">
    /// The name of the cache of the tokens stored in a named cache (see <see cref="Options.TokenCacheOptions.CacheName"/>), by cache key. A key that is not in it is in the
    /// token cache (the cache of the default <see cref="Options.TokenCacheOptions"/>); an index stored by a previous version has none. The index itself is always in the token cache.
    /// The cache name is kept rather than the name of the options, so a token is still found at sign-out when its options are changed or removed.
    /// </param>
    public record OboTokenCacheIndex(Dictionary<string, DateTime> Keys, Dictionary<string, string>? CacheNames = null)
    {
        /// <summary>Gets the name of the cache of a cached token, or <see langword="null"/> for the token cache.</summary>
        /// <param name="key">The cache key of the token.</param>
        /// <returns>The name of the cache.</returns>
        public string? GetCacheName(string key) => CacheNames is not null && CacheNames.TryGetValue(key, out var name) ? name : null;

        /// <summary>Gets the expiration of the last token: the index is kept until then. An empty index is already expired.</summary>
        /// <remarks>It is computed from <see cref="Keys"/>, so it is not stored in the cache.</remarks>
        [JsonIgnore]
        public DateTime ExpiresOnUtc => Keys.Count == 0 ? DateTime.MinValue : Keys.Values.Max();
    }
}
