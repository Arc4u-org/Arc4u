using System.Collections.Concurrent;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// An <see cref="ITokenCache"/> for a local client application: the tokens are persisted in the <see cref="ISecureCache"/> and kept in memory, by key, for the next reads.
/// </summary>
[Export(typeof(ITokenCache)), Shared]
public class ApplicationLocalDataCache : ITokenCache
{
    // The in-memory copies, by key: each key keeps its own token (a client token entry must not get the token of another one).
    private readonly ConcurrentDictionary<string, object> _tokens = new(StringComparer.Ordinal);
    private readonly ICache Cache;
    private readonly ILogger Logger;

    /// <summary>Initializes a new instance of the <see cref="ApplicationLocalDataCache"/> class.</summary>
    /// <param name="cache">The secure cache in which the tokens are persisted.</param>
    /// <param name="logger">The logger.</param>
    public ApplicationLocalDataCache(ISecureCache cache, ILogger logger)
    {
        Cache = cache;
        Logger = logger;
    }

    /// <summary>Removes the token from the secure cache and from the in-memory copies. Errors are ignored.</summary>
    /// <param name="key">The key of the token.</param>
    public void Clear(string key)
    {
        try
        {
            Cache.Remove(key);
            _tokens.TryRemove(key, out _);
        }
        catch
        {
            // Errors are ignored: clearing the token is best effort.
        }
    }

    /// <inheritdoc/>
    /// <remarks>The <paramref name="optionsName"/> is ignored: there is a single secure cache. Errors are ignored.</remarks>
    public async ValueTask DeleteItemAsync(string key, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _tokens.TryRemove(key, out _);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Errors are ignored: clearing the token is best effort.
        }
    }

    /// <inheritdoc/>
    /// <remarks>The <paramref name="optionsName"/> is ignored: there is a single secure cache. Errors are logged.</remarks>
    public async ValueTask PutAsync<T>(string key, T data, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Cache.PutAsync(key, data, cancellationToken).ConfigureAwait(false);
            if (data is null)
            {
                _tokens.TryRemove(key, out _);
            }
            else
            {
                _tokens[key] = data;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.Technical<ApplicationLocalDataCache>().LogException(ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The <paramref name="optionsName"/> is ignored: there is a single secure cache. The in-memory copy of the key is returned without reading the secure cache;
    /// errors are logged and give <see langword="null"/>.
    /// </remarks>
    public async ValueTask<T?> GetAsync<T>(string key, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(key, out var inMemory) && inMemory is T token)
        {
            return token;
        }

        try
        {
            var value = await Cache.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                _tokens[key] = value;
            }

            return value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.Technical<ApplicationLocalDataCache>().LogException(ex);
            return default;
        }
    }
}
