using System.Text.Json.Serialization;

namespace Arc4u.OAuth2.TokenProviders
{
    /// <summary>
    /// The cache keys of the on-behalf-of tokens obtained for one user, with their expiration, stored by <see cref="AzureADOboTokenProvider"/> so they can be removed when the user signs out.
    /// When the cache serializer uses a <c>JsonSerializerContext</c>, add this type to it.
    /// </summary>
    /// <param name="Keys">The cache keys of the tokens and their expiration date (UTC).</param>
    public record OboTokenCacheIndex(Dictionary<string, DateTime> Keys)
    {
        /// <summary>Gets the expiration of the last token: the index is kept until then. An empty index is already expired.</summary>
        /// <remarks>It is computed from <see cref="Keys"/>, so it is not stored in the cache.</remarks>
        [JsonIgnore]
        public DateTime ExpiresOnUtc => Keys.Count == 0 ? DateTime.MinValue : Keys.Values.Max();
    }
}
