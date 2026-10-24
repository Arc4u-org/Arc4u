using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Arc4u.Security.Cryptography;

/// <summary>
/// Extension methods to encrypt and decrypt strings with an <see cref="X509Certificate2"/>.
/// Short texts are encrypted directly with RSA (OAEP, SHA-256); longer texts are encrypted with a random AES key that is itself encrypted with the certificate.
/// The result is a base64 string, or two base64 parts separated by a dot (<c>key.data</c>) in the AES case, where <c>data</c> is produced by <see cref="CypherCodec.Encrypt(string, byte[])"/>.
/// With an EC certificate the text is encrypted with ECIES: an ephemeral ECDH key agreement with the certificate key, HKDF-SHA256 to derive the AES key,
/// then AES-GCM. The result is <c>ephemeralPublicKey.data</c>, the ephemeral public key being a base64 SubjectPublicKeyInfo.
/// The algorithm is chosen from the certificate key, so a text must be decrypted with the certificate it was encrypted for.
/// Only certificates with an RSA or an EC key are supported.
/// </summary>
public static class Certificate
{
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";
    private const int EcdhKeySizeInBytes = 32;
    private static readonly byte[] EcdhInfoLabel = "Arc4u.ECIES.v1"u8.ToArray();

    /// <summary>
    /// Encrypt a text and return an encrypted version formated in a 64string.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="x509">The certificate used to encrypt</param>
    /// <returns>The encrypted plain text.</returns>
    /// <exception cref="NotSupportedException">The certificate has no RSA or EC public key.</exception>
    public static string Encrypt(this X509Certificate2 x509, string plainText)
    {
        ArgumentNullException.ThrowIfNull(x509);

        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        if (IsEc(x509))
        {
            return EncryptWithEcdh(x509, plainText);
        }

        using var rsa = x509.GetRSAPublicKey() ?? throw NotSupportedKey(x509);

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

        var key = Convert.FromBase64String(CypherCodec.GenerateKey());

        var encryptedKey = rsa.Encrypt(key);
        var encryptedData = CypherCodec.Encrypt(plainText, key);

        return $"{encryptedKey}.{encryptedData}";

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
    /// <exception cref="CryptographicException">The certificate has no private key, or its private key cannot be accessed (for example missing permissions on a LocalMachine certificate).</exception>
    /// <exception cref="NotSupportedException">The certificate has no RSA or EC private key.</exception>
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

        if (IsEc(x509))
        {
            return DecryptWithEcdh(x509, base64Cypherstring);
        }

        using var rsa = GetPrivateKey(x509, x509.GetRSAPrivateKey);

        if (base64Cypherstring.Contains('.'))
        {
            var parts = base64Cypherstring.Split('.');
            if (parts.Length != 2)
            {
                throw new ApplicationException("Invalid encrypted string format");
            }

            var key = rsa.DecryptstringToBytes(parts[0]);

            return CypherCodec.Decrypt(parts[1], key);
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

    private static bool IsEc(X509Certificate2 x509) => x509.PublicKey.Oid.Value == EcPublicKeyOid;

    /// <summary>
    /// Encrypt with an ephemeral EC key: the AES key is derived from the ECDH agreement between the ephemeral key and the certificate key.
    /// </summary>
    private static string EncryptWithEcdh(X509Certificate2 x509, string plainText)
    {
        using var recipient = x509.GetECDiffieHellmanPublicKey() ?? throw NotSupportedKey(x509);
        using var recipientPublicKey = recipient.PublicKey;
        using var ephemeral = ECDiffieHellman.Create(recipient.ExportParameters(false).Curve);

        var ephemeralPublicKey = ephemeral.ExportSubjectPublicKeyInfo();
        var key = DeriveEcdhKey(ephemeral, recipientPublicKey, ephemeralPublicKey, x509);

        return $"{Convert.ToBase64String(ephemeralPublicKey)}.{CypherCodec.Encrypt(plainText, key)}";
    }

    private static string DecryptWithEcdh(X509Certificate2 x509, string cypherText)
    {
        var parts = cypherText.Split('.');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid encrypted string format");
        }

        using var privateKey = GetPrivateKey(x509, () => x509.GetECDiffieHellmanPrivateKey() ?? CopyEcdsaKeyForKeyAgreement(x509));

        var ephemeralPublicKey = Convert.FromBase64String(parts[0]);
        using var ephemeral = ECDiffieHellman.Create();
        ephemeral.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
        using var ephemeralKey = ephemeral.PublicKey;

        var key = DeriveEcdhKey(privateKey, ephemeralKey, ephemeralPublicKey, x509);

        return CypherCodec.Decrypt(parts[1], key);
    }

    /// <summary>
    /// On Windows, a CNG key created for ECDSA (the usual case for an EC certificate) cannot do a key agreement, so <c>GetECDiffieHellmanPrivateKey</c> returns null.
    /// The same private key is then copied into an <see cref="ECDiffieHellman"/>, which needs an exportable key
    /// (loaded from PEM files, or from a PFX with <see cref="X509KeyStorageFlags.Exportable"/>).
    /// </summary>
    private static ECDiffieHellman? CopyEcdsaKeyForKeyAgreement(X509Certificate2 x509)
    {
        using var ecdsa = x509.GetECDsaPrivateKey();
        if (ecdsa is null)
        {
            return null;
        }

        ECParameters parameters;
        try
        {
            parameters = ecdsa.ExportParameters(includePrivateParameters: true);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(string.Format(CultureInfo.InvariantCulture,
                                                           "The EC private key of the certificate {0} ({1}) is reserved for signatures on this platform and is not exportable, so it cannot be used for an ECDH key agreement. " +
                                                           "Load the certificate from PEM files or from a PFX with an exportable key.",
                                                           x509.Subject, x509.Thumbprint), ex);
        }

        try
        {
            return ECDiffieHellman.Create(parameters);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(parameters.D);
        }
    }

    /// <summary>
    /// Derive the AES key from the ECDH shared secret with HKDF-SHA256.
    /// The ephemeral and the certificate public keys are part of the HKDF info, so the key is bound to both (a substituted ephemeral key fails the AES-GCM tag check).
    /// </summary>
    private static byte[] DeriveEcdhKey(ECDiffieHellman ownKey, ECDiffieHellmanPublicKey otherKey, byte[] ephemeralPublicKey, X509Certificate2 x509)
    {
        var secret = ownKey.DeriveRawSecretAgreement(otherKey);
        try
        {
            byte[] info = [.. EcdhInfoLabel, .. ephemeralPublicKey, .. x509.PublicKey.ExportSubjectPublicKeyInfo()];

            return HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, EcdhKeySizeInBytes, salt: [], info);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>
    /// Get the private key and explain the failure when the key exists but cannot be opened,
    /// typically a certificate in the LocalMachine store whose private key is not readable by the current user ("Keyset does not exist").
    /// </summary>
    private static T GetPrivateKey<T>(X509Certificate2 x509, Func<T?> getPrivateKey) where T : AsymmetricAlgorithm
    {
        try
        {
            return getPrivateKey() ?? throw NotSupportedKey(x509);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(string.Format(CultureInfo.InvariantCulture,
                                                           "The private key of the certificate {0} ({1}) cannot be accessed: {2} " +
                                                           "When the certificate is in the LocalMachine store, the current user must be granted read access to its private key " +
                                                           "(Manage Private Keys in the certificate MMC), or use a certificate in the CurrentUser store or loaded from PEM files.",
                                                           x509.Subject, x509.Thumbprint, ex.Message), ex);
        }
    }

    private static NotSupportedException NotSupportedKey(X509Certificate2 x509)
        => new(string.Format(CultureInfo.InvariantCulture,
                             "The certificate {0} ({1}) uses the key algorithm {2}. Only certificates with an RSA or an EC key can encrypt or decrypt secrets.",
                             x509.Subject, x509.Thumbprint, x509.PublicKey.Oid.FriendlyName ?? x509.PublicKey.Oid.Value));
}
