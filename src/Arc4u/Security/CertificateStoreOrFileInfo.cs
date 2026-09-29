namespace Arc4u.Security;
/// <summary>
/// Describes where to find a certificate: either in a certificate store or in PEM files. The store takes precedence when both are provided.
/// </summary>
public class CertificateStoreOrFileInfo
{
    /// <summary>
    /// Gets or sets the description of a certificate located in a certificate store.
    /// </summary>
    public CertificateInfo? Store { get; set; }

    /// <summary>
    /// Gets or sets the description of a certificate stored in PEM files.
    /// </summary>
    public CertificateFilePathInfo? File { get; set; }
}
