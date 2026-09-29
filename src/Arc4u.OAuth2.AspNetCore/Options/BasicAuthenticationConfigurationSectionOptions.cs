namespace Arc4u.OAuth2.Options;
/// <summary>
/// The configuration section layout of the Basic authentication, bound from the section given to
/// <see cref="Arc4u.OAuth2.Middleware.BasicAuthenticationMiddlewareExtension.AddBasicAuthenticationSettings(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration, string, Arc4u.Security.Cryptography.IX509CertificateLoader?, bool)"/>
/// (<c>Authentication:Basic</c> by default).
/// </summary>
public class BasicAuthenticationConfigurationSectionOptions
{
    /// <summary>Gets or sets the path of the section that holds the <see cref="BasicSettingsOptions"/>. The default is <c>Authentication:Basic:Settings</c>.</summary>
    public string BasicSettingsPath { get; set; } = "Authentication:Basic:Settings";

    /// <summary>Gets or sets the domain suffix appended to a user name that has no domain, for example <c>@contoso.com</c>. Optional.</summary>
    public string DefaultUpn { get; set; } = default!;

    /// <summary>
    /// Gets or sets the path of the section that maps a header name to the certificate (store or file) used to decrypt the credentials it carries.
    /// The default is <c>Authentication:Basic:Certificates</c>.
    /// </summary>
    public string CertificateHeaderPath { get; set; } = "Authentication:Basic:Certificates";
}
