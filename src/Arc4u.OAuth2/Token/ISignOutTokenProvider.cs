namespace Arc4u.OAuth2.Token;

/// <summary>
/// An <see cref="ITokenProvider"/> that holds tokens for a user session and removes them when the user signs out.
/// Providers without a user session (machine to machine, a secret given by the settings, the token of the incoming request...) only implement <see cref="ITokenProvider"/>.
/// </summary>
public interface ISignOutTokenProvider : ITokenProvider
{
    /// <summary>
    /// Signs the user out and removes the tokens this provider holds for the user.
    /// It is called for every user sign-out (see <see cref="SignOutTokenProviderExtensions.SignOutTokenProvidersAsync"/>), with the same settings for every provider:
    /// the settings can be empty or belong to another provider. In that case, remove what can be identified without them, or do nothing, but do not throw.
    /// </summary>
    /// <param name="settings">The settings given to the sign-out, which may not be the settings of this provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the sign-out is done.</returns>
    ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken);
}
