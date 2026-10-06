using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Arc4u.Security.Cryptography;

/// <summary>
/// Extension methods to encrypt and decrypt strings with an <see cref="X509Certificate2"/>.
/// Short texts are encrypted directly with RSA (OAEP, SHA-256); longer texts are encrypted with a random AES key that is itself encrypted with the certificate.
/// The result is a base64 string, or three base64 parts separated by dots (<c>key.iv.data</c>) in the AES case.
/// Only certificates with an RSA key are supported.
/// </summary>
public static class Certificate
{
    /// <summary>
    /// Encrypt a text and return an encrypted version formated in a 64string.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="x509">The certificate used to encrypt</param>
    /// <returns>The encrypted plain text.</returns>
    /// <exception cref="NotSupportedException">The certificate has no RSA public key.</exception>
    public static string Encrypt(this X509Certificate2 x509, string plainText)
    {
        ArgumentNullException.ThrowIfNull(x509);

        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        using var rsa = x509.GetRSAPublicKey() ?? throw NotRsa(x509);

        var bytes = Encoding.UTF8.GetBytes(plainText.Trim());

        try
        {
            if (bytes.Length < 191)
            {
                return rsa.Encrypt(bytes);
            }
        }
        catch (Exception)
        {
            // will use the Aes encryption.
            // Encapsulate in case of on an unexpected behaviour on an non tested platform.
        }

        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        var encryptedKey = rsa.Encrypt(aes.Key);
        var encryptedIv = rsa.Encrypt(aes.IV);
        var encryptedData = CypherCodec.EncodeClearText(plainText, aes.Key, aes.IV);

        return $"{encryptedKey}.{encryptedIv}.{encryptedData}";

    }

    /// <summary>
    /// Encrypt a byte array and return an encrypted version formated in a base64string.
    /// </summary>
    /// <param name="content">The binary content to encrypt.</param>
    /// <param name="rsa">The RSA public key used to encrypt</param>
    /// <returns>The encrypted plain text.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    private static string Encrypt(this RSA rsa, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return Convert.ToBase64String(rsa.Encrypt(content, RSAEncryptionPadding.OaepSHA256));
    }

    /// <summary>
    /// Decrypt an formated 64 string encrypted and return the text in clear.
    /// </summary>
    /// <param name="base64Cypherstring">The cypher text to decrypt in a base 64 format.</param>
    /// <param name="x509">The certificate used to encrypt</param>
    /// <returns>The decrypted text.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="CryptographicException">The certificate has no private key.</exception>
    /// <exception cref="NotSupportedException">The certificate has no RSA private key.</exception>
    public static string Decrypt(this X509Certificate2 x509, string base64Cypherstring)
    {
        ArgumentNullException.ThrowIfNull(x509);

        if (string.IsNullOrWhiteSpace(base64Cypherstring))
        {
            throw new ArgumentNullException(nameof(base64Cypherstring));
        }

        if (!x509.HasPrivateKey)
        {
            throw new CryptographicException(string.Format(CultureInfo.InvariantCulture, "The certificate {0} has no private key!", x509.FriendlyName));
        }

        using var rsa = x509.GetRSAPrivateKey() ?? throw NotRsa(x509);

        if (base64Cypherstring.Contains('.'))
        {
            var parts = base64Cypherstring.Split('.');
            if (parts.Length != 3)
            {
                throw new ApplicationException("Invalid encrypted string format");
            }

            var key = rsa.DecryptstringToBytes(parts[0]);
            var iv = rsa.DecryptstringToBytes(parts[1]);

            return CypherCodec.DecodeCypherstring(parts[2], key, iv);
        }

        return Encoding.UTF8.GetString(rsa.DecryptstringToBytes(base64Cypherstring));
    }

    /// <summary>
    /// Decrypt an formated 64 string encrypted and return the array of bytes.
    /// </summary>
    /// <param name="base64Cypherstring">The cypher text to decrypt in a base64 format.</param>
    /// <param name="rsa">The RSA private key used to decrypt</param>
    /// <returns>The decrypted byte array.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    private static byte[] DecryptstringToBytes(this RSA rsa, string base64Cypherstring)
    {
        if (string.IsNullOrWhiteSpace(base64Cypherstring))
        {
            throw new ArgumentNullException(nameof(base64Cypherstring));
        }

        var cipherBytes = Convert.FromBase64String(base64Cypherstring);

        return rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
    }

    private static NotSupportedException NotRsa(X509Certificate2 x509)
        => new(string.Format(CultureInfo.InvariantCulture,
                             "The certificate {0} ({1}) uses the key algorithm {2}. Only certificates with an RSA key can encrypt or decrypt secrets.",
                             x509.Subject, x509.Thumbprint, x509.PublicKey.Oid.FriendlyName ?? x509.PublicKey.Oid.Value));
}
