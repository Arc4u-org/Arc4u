using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Arc4u.OAuth2.TicketStore;

/// <summary>Tells whether two authentication tickets belong to the same user, so a ticket store never gives one user's session to another.</summary>
internal static class AuthenticationTicketSubject
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
    /// Returns <see langword="false"/> when an identifier claim present in both tickets has different values. When no identifier claim can be
    /// compared, the tickets are considered to belong to the same user: the check only blocks a proven mismatch.
    /// </summary>
    internal static bool IsSameSubject(AuthenticationTicket stored, AuthenticationTicket renewed)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(renewed);

        foreach (var type in IdentifierClaimTypes)
        {
            var storedValue = stored.Principal.FindFirst(type)?.Value;
            var renewedValue = renewed.Principal.FindFirst(type)?.Value;
            if (storedValue is not null && renewedValue is not null && !string.Equals(storedValue, renewedValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
