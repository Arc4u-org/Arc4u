using Arc4u.OAuth2.Options;

namespace Arc4u.OAuth2;
/// <summary>
/// The settings used by the Basic authentication to request a token from an authority for the credentials received in a request.
/// Bound from the <c>Authentication:Basic:Settings</c> section by default.
/// </summary>
public class BasicSettingsOptions
{
    /// <summary>Gets or sets the key of the token provider that requests the token. The default is <c>Credential</c>.</summary>
    public string ProviderId { get; set; } = "Credential";

    /// <summary>Gets or sets the authentication type of the resulting identity. The default is <see cref="Constants.InjectAuthenticationType"/>.</summary>
    public string AuthenticationType { get; set; } = Constants.InjectAuthenticationType;

    /// <summary>Gets or sets the authority to call. When <see langword="null"/>, the default authority is used.</summary>
    public AuthorityOptions? Authority { get; set; } = default!;

    /// <summary>Gets or sets the client id registered in the authority. Required.</summary>
    public string ClientId { get; set; } = default!;

    /// <summary>Gets or sets the requested scopes. At least one is required.</summary>
    public List<string> Scopes { get; set; } = [];

    // Some STS are using user name - password in combination with a client secret (kind of 2 factor authentication).
    /// <summary>Gets or sets the optional client secret. Some authorities require it in combination with the user name and password.</summary>
    public string? ClientSecret { get; set; } = default!;
}
