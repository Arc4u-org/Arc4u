using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;

namespace Arc4u.OAuth2.Token;

/// <summary>Extension methods reading the access token expiration from the <c>exp</c> claim of an identity.</summary>
public static class UserProfileExt
{
    /// <summary>The claim type (<c>exp</c>) holding the expiration date of the access token, in Unix seconds.</summary>
    public static readonly string tokenExpirationClaimType = "exp";

    /// <summary>Gets the expiration date of the access token, from its <c>exp</c> claim.</summary>
    /// <param name="identity">The identity, which must be a <see cref="ClaimsIdentity"/>.</param>
    /// <returns>The expiration date. The Unix epoch is returned when the identity has no valid <c>exp</c> claim.</returns>
    /// <exception cref="InvalidOperationException">The identity is not a <see cref="ClaimsIdentity"/>.</exception>
    public static DateTime AccessTokenExpiresOn(this IIdentity identity)
    {
        if (identity is not ClaimsIdentity claimsIdentity)
        {
            throw new InvalidOperationException("The identity is not a ClaimsIdentity.");
        }
        return GetExpDateTimeOffset(claimsIdentity).DateTime;
    }

    /// <summary>Gets the expiration date of the access token in UTC, from its <c>exp</c> claim.</summary>
    /// <param name="identity">The identity, which must be a <see cref="ClaimsIdentity"/>.</param>
    /// <returns>The expiration date in UTC. The Unix epoch is returned when the identity has no valid <c>exp</c> claim.</returns>
    /// <exception cref="InvalidOperationException">The identity is not a <see cref="ClaimsIdentity"/>.</exception>
    public static DateTime AccessTokenExpiresOnUtc(this IIdentity identity)
    {
        if (identity is not ClaimsIdentity claimsIdentity)
        {
            throw new InvalidOperationException("The identity is not a ClaimsIdentity.");
        }
        return GetExpDateTimeOffset(claimsIdentity).UtcDateTime;
    }

    private static DateTimeOffset GetExpDateTimeOffset(ClaimsIdentity identity)
    {
        if (null != identity)
        {
            var expTokenClaim = identity.Claims.FirstOrDefault(c => c.Type.Equals(tokenExpirationClaimType, StringComparison.InvariantCultureIgnoreCase));
            long expTokenTicks = 0;
            if (null != expTokenClaim)
            {
                long.TryParse(expTokenClaim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out expTokenTicks);

                return DateTimeOffset.FromUnixTimeSeconds(expTokenTicks);
            }
        }
        return DateTimeOffset.FromUnixTimeSeconds(0);
    }
}
