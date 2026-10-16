namespace Arc4u.Security;

/// <summary>
/// The key used to encrypt and decrypt with <see cref="Arc4u.Security.Cryptography.CypherCodec"/>.
/// </summary>
public class CypherCodecConfig
{
    /// <summary>
    /// Gets or sets the base64 encoded key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
