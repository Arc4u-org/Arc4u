using System.Security.Cryptography;
using System.Text;

namespace Arc4u.Security.Cryptography;

/// <summary>
/// Extension methods for <see cref="HashAlgorithm"/>.
/// </summary>
public static class HashAlgorithmExtensions
{
    /// <summary>
    /// Computes the hash of a string.
    /// </summary>
    /// <param name="algorithm">The hash algorithm.</param>
    /// <param name="input">The string to hash.</param>
    /// <param name="encoding">The encoding used to convert <paramref name="input"/> into bytes.</param>
    /// <returns>The hash as hexadecimal pairs separated by hyphens (the format of <see cref="BitConverter.ToString(byte[])"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
    public static string ComputeHash(this HashAlgorithm algorithm, string input, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(encoding);

        //convert the string into an array of bytes.
        var messageBytes = encoding.GetBytes(input);

        //create the hash value from the array of bytes.
        var hashValue = algorithm.ComputeHash(messageBytes);

        //convert the hash value to string
        return BitConverter.ToString(hashValue);
    }
}
