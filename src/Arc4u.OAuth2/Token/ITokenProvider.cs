using FluentResults;

namespace Arc4u.OAuth2.Token;

/// <summary>Provides an access token for a set of settings.</summary>
public interface ITokenProvider
{
    /// <summary>Gets an access token.</summary>
    /// <param name="settings">The provider settings (see <see cref="TokenKeys"/>).</param>
    /// <param name="platformParameters">Provider specific data, for example the <c>ClaimsIdentity</c> of the user.</param>
    /// <returns>The token, or a failed result when it cannot be obtained.</returns>
    Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters);

    /// <summary>Signs the user out and removes his tokens.</summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the sign-out is done.</returns>
    ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken);
}
