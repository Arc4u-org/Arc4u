using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Arc4u.OAuth2.Token;

/// <summary>A token with its type and expiration date.</summary>
[DataContract]
public class TokenInfo
{
    /// <summary>Initializes a new instance of the <see cref="TokenInfo"/> class with an explicit expiration date.</summary>
    /// <param name="tokenType">The token type, for example <c>Bearer</c>.</param>
    /// <param name="token">The token value.</param>
    /// <param name="expiresOnUtc">The expiration date. It is converted to UTC.</param>
    [JsonConstructor]
    public TokenInfo(string tokenType, string token, DateTime expiresOnUtc)
    {
        TokenType = tokenType;
        Token = token;
        ExpiresOnUtc = expiresOnUtc.ToUniversalTime();
    }

    /// <summary>Initializes a new instance of the <see cref="TokenInfo"/> class from a JWT; the expiration date is the <c>ValidTo</c> of the token.</summary>
    /// <param name="tokenType">The token type, for example <c>Bearer</c>.</param>
    /// <param name="token">The JWT.</param>
    public TokenInfo(string tokenType, string token)
    {
        TokenType = tokenType;
        Token = token;

        var jwtToken = new JwtSecurityToken(token);
        ExpiresOnUtc = jwtToken.ValidTo.ToUniversalTime();
    }

    /// <summary>
    /// Gets the type of the Access Token returned. 
    /// </summary>
    [DataMember]
    public string TokenType { get; private set; }

    /// <summary>
    /// Gets the Access Token requested.
    /// </summary>
    [DataMember]
    public string Token { get; internal set; }

    /// <summary>
    /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
    /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
    /// </summary>
    [DataMember]
    public DateTime ExpiresOnUtc { get; internal set; }

}
