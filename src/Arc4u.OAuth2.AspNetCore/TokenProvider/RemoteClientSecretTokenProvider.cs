using System.Text;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using FluentResults;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>
/// An <see cref="ITokenProvider"/> that does not call an authority: it wraps the remote secret of the settings (<see cref="TokenKeys.ClientSecret"/>) in a <see cref="TokenInfo"/> valid for one hour,
/// using the header name of the settings (<see cref="TokenKeys.ClientSecretHeader"/>) as token type.
/// When the header is <c>Basic</c>, a clear <c>user:password</c> secret is Base64 encoded; a secret already Base64 encoded is sent as is.
/// </summary>
[Export(RemoteClientSecretTokenProvider.ProviderName, typeof(ITokenProvider)), Shared]
public class RemoteClientSecretTokenProvider : ITokenProvider
{
    /// <summary>The key (<c>RemoteSecret</c>) under which the provider is registered.</summary>
    public const string ProviderName = "RemoteSecret";

    /// <summary>Gets the token wrapping the remote secret.</summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="_">Not used.</param>
    /// <returns>The token, or a failed result when the header name is not in the settings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">The settings have no client secret.</exception>
    public Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? _)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Read the settings to extract the data:
        // HeaderKey => default = SecretKey
        // ClientSecret: the encrypted username/password.

        if (!settings.Values.ContainsKey(TokenKeys.ClientSecretHeader))
        {

            return Task.FromResult(Result.Fail<TokenInfo>(new Error("Client secret Header is missing. Cannot process the request.")));
        }

        if (!settings.Values.ContainsKey(TokenKeys.ClientSecret))
        {
            throw new ConfigurationException("Client secret is missing. Cannot process the request.");
        }

        var headerKey = settings.Values[TokenKeys.ClientSecretHeader];
        var clientSecret = settings.Values[TokenKeys.ClientSecret];

        // A Basic Authorization header carries base64(user:password). The secret can be configured in clear text or already encoded.
        // A custom header carries the encrypted secret that the server decrypts: it is sent unchanged.
        if (BasicScheme.Equals(headerKey, StringComparison.OrdinalIgnoreCase) && !IsBase64(clientSecret))
        {
            clientSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientSecret));
        }

        return Task.FromResult(Result.Ok(new TokenInfo(headerKey, clientSecret, DateTime.UtcNow + TimeSpan.FromHours(1))));

    }

    private const string BasicScheme = "Basic";

    // A clear user:password pair always contains a ':', which is not a Base64 character.
    private static bool IsBase64(string value)
    {
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    /// <summary>Not supported.</summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
