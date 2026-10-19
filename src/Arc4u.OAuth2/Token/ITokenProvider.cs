using FluentResults;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// Provides an access token for a set of settings.
/// A provider that holds tokens for a user session (and must remove them when the user signs out) also implements <see cref="ISignOutTokenProvider"/>.
/// </summary>
public interface ITokenProvider
{
    /// <summary>Gets an access token.</summary>
    /// <param name="settings">The provider settings (see <see cref="TokenKeys"/>).</param>
    /// <param name="platformParameters">Provider specific data, for example the <c>ClaimsIdentity</c> of the user.</param>
    /// <returns>The token, or a failed result when it cannot be obtained.</returns>
    ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters);
}
