namespace Arc4u.OAuth2.Token;

/// <summary>Holds the access token and the refresh token obtained from an identity provider.</summary>
public record TokenRefreshInfo
{
    /// <summary>Gets or sets the access token.</summary>
    public TokenInfo? AccessToken { get; set; }
    /// <summary>Gets or sets the refresh token.</summary>
    public TokenInfo? RefreshToken { get; set; }
    /// <summary>
    /// Gets or sets the identity token of the login, sent as <c>id_token_hint</c> when the user signs out so the authority ends that session.
    /// It is kept by the client providers only.
    /// </summary>
    public string? IdentityToken { get; set; }

}
