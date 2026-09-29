using System.Security.Cryptography.X509Certificates;

namespace Arc4u.OAuth2.Options;
/// <summary>
/// The code based configuration of the Basic authentication middleware, given to
/// <see cref="Arc4u.OAuth2.Middleware.BasicAuthenticationMiddlewareExtension.AddBasicAuthenticationSettings(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{BasicAuthenticationConfigurationOptions})"/>.
/// </summary>
public class BasicAuthenticationConfigurationOptions
{
    /// <summary>Gets or sets the action that configures the <see cref="BasicSettingsOptions"/> used to request a token for the credentials received.</summary>
    public Action<BasicSettingsOptions> BasicOptions { get; set; } = default!;

    /// <summary>
    /// Gets or sets the domain suffix appended to a user name that has no domain. It must start with <c>@</c> and contain a valid host name, for example <c>@contoso.com</c>.
    /// The value is optional.
    /// </summary>
    public string DefaultUpn { get; set; } = default!;

    /// <summary>
    /// Gets or sets the action that fills the certificates used to decrypt the credentials sent in a request header or query string parameter.
    /// The key of each entry is the name of the header (or query string parameter) that carries the encrypted <c>user:password</c> pair.
    /// </summary>
    public Action<Dictionary<string, X509Certificate2>> CertificateHeaderOptions { get; set; } = default!;
}
