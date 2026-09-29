namespace Arc4u.Security;
/// <summary>
/// Describes a certificate stored in two PEM files: the public certificate and its private key.
/// </summary>
public record CertificateFilePathInfo
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    /// <summary>
    /// Gets or sets the path of the PEM file containing the public certificate.
    /// </summary>
    public string Cert { get; set; }
    /// <summary>
    /// Gets or sets the path of the PEM file containing the private key.
    /// </summary>
    public string Key { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
