using System.Security.Cryptography;
using System.Text;

namespace Arc4u.Security.Cryptography;

/// <summary>
/// Encrypts and decrypts strings with AES-GCM.
/// A random nonce is generated for each value, so the same key can safely protect many values,
/// and any modification of the cypher text (or a wrong key) is detected when decrypting.
/// </summary>
public static class CypherCodec
{
    private static readonly Encoding _cypherEncoding = Encoding.UTF8;

    private const int KeySizeInBytes = 32;
    private static readonly int NonceSize = AesGcm.NonceByteSizes.MaxSize;
    private static readonly int TagSize = AesGcm.TagByteSizes.MaxSize;

    /// <summary>
    /// Generates a random 256 bits key to use with <see cref="Encrypt(string, byte[])"/>.
    /// </summary>
    /// <returns>The base64 encoded key.</returns>
    public static string GenerateKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySizeInBytes));
    }

    /// <summary>
    /// Encrypts a text with AES-GCM. A new random nonce is generated for each call.
    /// </summary>
    /// <param name="clearText">The clear text.</param>
    /// <param name="key">The AES key (16, 24 or 32 bytes).</param>
    /// <returns>The base64 of nonce, tag and cypher text.</returns>
    public static string Encrypt(string clearText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(clearText);
        ArgumentNullException.ThrowIfNull(key);

        var plain = _cypherEncoding.GetBytes(clearText);

        var result = new byte[NonceSize + TagSize + plain.Length];
        var nonce = result.AsSpan(0, NonceSize);
        var tag = result.AsSpan(NonceSize, TagSize);
        var cypher = result.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cypher, tag);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts a text produced by <see cref="Encrypt(string, byte[])"/>.
    /// </summary>
    /// <param name="cypherText">The base64 cypher text.</param>
    /// <param name="key">The AES key.</param>
    /// <returns>The clear text.</returns>
    /// <exception cref="FormatException">The cypher text is not in the expected format.</exception>
    /// <exception cref="AuthenticationTagMismatchException">The key is wrong or the cypher text was modified.</exception>
    public static string Decrypt(string cypherText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(cypherText);
        ArgumentNullException.ThrowIfNull(key);

        var data = Convert.FromBase64String(cypherText);

        if (data.Length < NonceSize + TagSize)
        {
            throw new FormatException("The cypher text is too short.");
        }

        var plain = new byte[data.Length - NonceSize - TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(data.AsSpan(0, NonceSize), data.AsSpan(NonceSize + TagSize), data.AsSpan(NonceSize, TagSize), plain);

        return _cypherEncoding.GetString(plain);
    }
}
