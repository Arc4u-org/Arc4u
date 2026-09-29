namespace Arc4u.OAuth2.Token;

/// <summary>A store for the tokens of the current user.</summary>
public interface ITokenCache
{
    /// <summary>
    /// Delete a token based on its unique key.
    /// </summary>
    /// <param name="key"></param>
    void DeleteItem(string key);

    /// <summary>Adds or replaces a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <param name="data">The value to store. A <see langword="null"/> value is ignored by some implementations.</param>
    void Put<T>(string key, T data);

    /// <summary>Gets a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The unique key.</param>
    /// <returns>The value, or the default of <typeparamref name="T"/> when there is none.</returns>
    T? Get<T>(string key);
}
