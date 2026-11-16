namespace Arc4u.OAuth2.Token;

/// <summary>A store for the tokens of the current user.</summary>
/// <remarks>
/// Each method accepts an optional <c>optionsName</c>: the name of the named <see cref="Options.TokenCacheOptions"/> that selects the cache
/// and the expiration to use. When it is <see langword="null"/>, the default <see cref="Options.TokenCacheOptions"/> (registered by <c>AddTokenCache</c>) are used.
/// Implementations that have a single store ignore it.
/// <para>
/// The methods are asynchronous because the store is usually a distributed cache (Redis, SQL Server): a call must not block a thread during the round trip.
/// </para>
/// </remarks>
public interface ITokenCache
{
    /// <summary>
    /// Delete a token based on its unique key.
    /// </summary>
    /// <param name="key">The unique key of the token.</param>
    /// <param name="optionsName">The name of the <see cref="Options.TokenCacheOptions"/> to use, or <see langword="null"/> for the default ones.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the entry is removed.</returns>
    ValueTask DeleteItemAsync(string key, string? optionsName = null, CancellationToken cancellationToken = default);

    /// <summary>Adds or replaces a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <param name="data">The value to store. A <see langword="null"/> value is ignored by some implementations.</param>
    /// <param name="optionsName">The name of the <see cref="Options.TokenCacheOptions"/> to use, or <see langword="null"/> for the default ones.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the value is stored.</returns>
    ValueTask PutAsync<T>(string key, T data, string? optionsName = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <param name="optionsName">The name of the <see cref="Options.TokenCacheOptions"/> to use, or <see langword="null"/> for the default ones.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="T"/> when there is none.</returns>
    ValueTask<T?> GetAsync<T>(string key, string? optionsName = null, CancellationToken cancellationToken = default);
}
