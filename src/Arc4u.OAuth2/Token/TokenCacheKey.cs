using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// Builds the hashed part of a token cache key.
/// <para>
/// The token cache can be distributed (Redis, SQL Server) or persistent, so a key must be the same in every
/// process and after a restart: <see cref="string.GetHashCode()"/> is randomized per process and must not be used.
/// Secrets (passwords, client secrets, user tokens) must not appear in clear text in a key either, since keys are logged.
/// </para>
/// </summary>
public static class TokenCacheKey
{
    /// <summary>
    /// Returns the upper-case hexadecimal SHA-256 of <paramref name="parts"/>. Each part is prefixed with its length so
    /// that different splits of the same characters (<c>"ab","c"</c> and <c>"a","bc"</c>) give different hashes.
    /// A <see langword="null"/> part is hashed as an empty string.
    /// </summary>
    /// <param name="parts">Every value that changes the token.</param>
    /// <returns>The hash, stable across processes and restarts.</returns>
    public static string Hash(params string?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            var value = part ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
