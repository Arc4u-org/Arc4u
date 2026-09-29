using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration;

namespace Arc4u.OAuth2.Options;
/// <summary>The validated Basic authentication settings, registered as options and consumed by the <c>BasicAuthenticationMiddleware</c>.</summary>
public class BasicAuthenticationSettingsOptions
{
    /// <summary>Gets or sets the token provider settings built from <see cref="BasicSettingsOptions"/> (provider id, client id, scope, ...).</summary>
    public SimpleKeyValueSettings BasicSettings { get; set; } = default!;

    /// <summary>Gets or sets the domain suffix appended to a user name that has no domain.</summary>
    public string DefaultUpn { get; set; } = default!;

    /// <summary>Gets or sets the certificates, by header name, used to decrypt the credentials sent in a header or query string parameter.</summary>
    public Dictionary<string, X509Certificate2> CertificateHeaderOptions { get; set; } = default!;
}
