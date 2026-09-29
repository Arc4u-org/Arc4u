using Arc4u.OAuth2.TokenProvider;

namespace Arc4u.OAuth2.Options;
/// <summary>
/// Describes a remote secret: an encrypted credential that a caller sends in a header and that the server decrypts and exchanges for a token.
/// Each entry of the <c>Authentication:RemoteSecrets</c> section is bound to this class.
/// </summary>
public class RemoteSecretSettingsOptions
{
    /// <summary>Gets or sets the key of the token provider handling the secret. The default is <see cref="RemoteClientSecretTokenProvider.ProviderName"/>.</summary>
    public string ProviderId { get; set; } = RemoteClientSecretTokenProvider.ProviderName;

    /// <summary>Gets or sets the name of the header that carries the secret. The default is <c>SecretKey</c>.</summary>
    public string HeaderKey { get; set; } = "SecretKey";

    /// <summary>Gets or sets the secret (the encrypted <c>user:password</c> pair). Required.</summary>
    public string ClientSecret { get; set; } = default!;

    /// <summary>Gets or sets the authentication type of the resulting identity. The default is <see cref="Constants.InjectAuthenticationType"/>.</summary>
    public string AuthenticationType { get; set; } = Constants.InjectAuthenticationType;

}
