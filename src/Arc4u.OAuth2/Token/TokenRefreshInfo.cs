namespace Arc4u.OAuth2.Token;

/// <summary>Holds the access token and the refresh token obtained from an identity provider.</summary>
public record TokenRefreshInfo
{
    /// <summary>Gets or sets the access token.</summary>
    public TokenInfo? AccessToken { get; set; }
    /// <summary>Gets or sets the refresh token.</summary>
    public TokenInfo? RefreshToken { get; set; }

}
