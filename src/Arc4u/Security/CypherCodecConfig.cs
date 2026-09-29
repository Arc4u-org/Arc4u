namespace Arc4u.Security;

/// <summary>
/// The key and initialization vector used to encrypt and decrypt with <see cref="Arc4u.Security.Cryptography.CypherCodec"/>.
/// </summary>
public class CypherCodecConfig
{
    /// <summary>
    /// Gets or sets the base64 encoded key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base64 encoded initialization vector.
    /// </summary>
    public string IV { get; set; } = string.Empty;
}
