using Arc4u.OAuth2.Security.Principal;
using FluentResults;

namespace Arc4u.OAuth2.Token;

/// <summary>Requests an access token for a user from his credentials.</summary>
public interface ICredentialTokenProvider
{
    /// <summary>Requests an access token for the credentials.</summary>
    /// <param name="settings">The provider settings (authority, client id, scope...). See <see cref="TokenKeys"/>.</param>
    /// <param name="credential">The user credentials.</param>
    /// <returns>The token, or a failed result when it cannot be obtained.</returns>
    Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings settings, CredentialsResult credential);
}
