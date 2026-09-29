using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Arc4u.Security.Principal;

[JsonSerializable(typeof(Authorization))]
internal partial class AuthorizationJsonContext : JsonSerializerContext
{
}

/// <summary>
/// An <see cref="IClaimAuthorizationFiller"/> that reads the <see cref="Arc4u.IdentityModel.Claims.ClaimTypes.Authorization"/> claim of a
/// <see cref="System.Security.Claims.ClaimsIdentity"/> and deserializes it into an <see cref="Authorization"/>.
/// </summary>
/// <param name="logger">The logger used to trace a claim that cannot be deserialized.</param>
[Export(typeof(IClaimAuthorizationFiller)), Shared]
public class ClaimsAuthorizationFiller(ILogger<ClaimsAuthorizationFiller> logger) : IClaimAuthorizationFiller
{
    /// <summary>
    /// Gets the authorization data of the identity.
    /// </summary>
    /// <param name="identity">The identity, which must be a <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
    /// <returns>The authorization contained in the authorization claim, or an empty <see cref="Authorization"/> when the claim is missing or cannot be deserialized.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException"><paramref name="identity"/> is not a <see cref="System.Security.Claims.ClaimsIdentity"/>.</exception>
    public Authorization GetAuthorization(System.Security.Principal.IIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (identity is not ClaimsIdentity)
        {
            throw new NotSupportedException("Only identity from ClaimsIdentity are allowed.");
        }

        var claimsIdentity = (ClaimsIdentity)identity;

        // Create a UserProfile based on the identity received.
        var claimAuthorization = ExtractClaimValue(IdentityModel.Claims.ClaimTypes.Authorization, claimsIdentity.Claims);

        if (!string.IsNullOrWhiteSpace(claimAuthorization))
        {
            return GetAuthorization(claimAuthorization) ?? new Authorization();
        }

        return new Authorization();
    }

    private Authorization? GetAuthorization(string claimAuthorization)
    {
        try
        {
            return JsonSerializer.Deserialize(claimAuthorization, AuthorizationJsonContext.Default.Authorization);
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
        }

        return new Authorization();
    }

    private static string ExtractClaimValue(string claimType, IEnumerable<Claim> claims)
    {
        var claim = claims.SingleOrDefault(c => c.Type.Equals(claimType, StringComparison.CurrentCultureIgnoreCase));
        try
        {
            if (null != claim)
            {
                return claim.Value ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
