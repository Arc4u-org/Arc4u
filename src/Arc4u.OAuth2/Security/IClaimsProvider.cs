using Arc4u.IdentityModel.Claims;

namespace Arc4u.OAuth2.Security;

/// <summary>
/// The IClaimsProvider is used to implement a backend service to provide extra claims.
/// By sample, custom rights or user information based on the user id (from a db call or other).
/// </summary>
public interface IClaimsProvider
{
    /// <summary>Gets the extra claims of a user.</summary>
    /// <param name="userIdentifier">The identifier of the user (see <c>ClaimsIdentifierOption</c>).</param>
    /// <returns>The extra claims of the user.</returns>
    Task<IEnumerable<ClaimDto>> GetAsync(string userIdentifier);
}
