namespace Arc4u.Security.CustomRootCA;

/// <summary>
/// Describes a custom root certificate authority. Only one of <see cref="CaPem"/>, <see cref="CaFilePath"/> and <see cref="Store"/> must be defined.
/// </summary>
public sealed class CARootOption
{
    /// <summary>
    /// Gets or sets the PEM text of the root certificate.
    /// </summary>
    public string? CaPem { get; set; }

    /// <summary>
    /// Gets or sets the path of a file containing the PEM text of the root certificate.
    /// </summary>
    public string? CaFilePath { get; set; }

    /// <summary>
    /// Gets or sets the description of a root certificate located in a certificate store.
    /// </summary>
    public CertificateInfo? Store { get; set; }
};
