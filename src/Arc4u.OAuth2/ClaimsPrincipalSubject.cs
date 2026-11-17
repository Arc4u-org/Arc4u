using System.Security.Claims;

namespace Arc4u.OAuth2;

/// <summary>Tells whether two principals belong to the same user, from the claims that identify the user.</summary>
public static class ClaimsPrincipalSubject
{
    // The claims identifying the user, as issued with MapInboundClaims = false (sub, oid) or mapped (the long forms).
    private static readonly string[] IdentifierClaimTypes =
    [
        "sub",
        "oid",
        ClaimTypes.NameIdentifier,
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
    ];

    /// <summary>
    /// Returns <see langword="false"/> when an identifier claim (<c>sub</c>, <c>oid</c>, or their mapped forms) present in both principals has
    /// different values. When no identifier claim can be compared, the principals are considered to belong to the same user: the check only
    /// detects a proven mismatch.
    /// </summary>
    /// <param name="first">The first principal.</param>
    /// <param name="second">The second principal.</param>
    /// <returns><see langword="false"/> when the principals are proven to belong to different users; otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.</exception>
    public static bool IsSameSubject(ClaimsPrincipal first, ClaimsPrincipal second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        foreach (var type in IdentifierClaimTypes)
        {
            var firstValue = first.FindFirst(type)?.Value;
            var secondValue = second.FindFirst(type)?.Value;
            if (firstValue is not null && secondValue is not null && !string.Equals(firstValue, secondValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
