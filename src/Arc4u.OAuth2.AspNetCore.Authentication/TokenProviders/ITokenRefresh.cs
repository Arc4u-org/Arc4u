namespace Arc4u.OAuth2.Token
{
    /// <summary>Refreshes the access token of the current user with his refresh token.</summary>
    public interface ITokenRefreshProvider
    {
        /// <summary>Requests a new access token, and a new refresh token when the authority gives one, and updates the scoped <see cref="TokenRefreshInfo"/>.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The updated <see cref="TokenRefreshInfo"/>.</returns>
        Task<TokenRefreshInfo?> RefreshTokenAsync(CancellationToken cancellationToken);
    }
}
