using Microsoft.AspNetCore.Authentication;

namespace Arc4u.OAuth2.TicketStore;

/// <summary>Tells whether two authentication tickets belong to the same user, so a ticket store never gives one user's session to another.</summary>
internal static class AuthenticationTicketSubject
{
    /// <summary>
    /// Returns <see langword="false"/> when an identifier claim present in both tickets has different values. When no identifier claim can be
    /// compared, the tickets are considered to belong to the same user: the check only blocks a proven mismatch (see <see cref="ClaimsPrincipalSubject"/>).
    /// </summary>
    internal static bool IsSameSubject(AuthenticationTicket stored, AuthenticationTicket renewed)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(renewed);

        return ClaimsPrincipalSubject.IsSameSubject(stored.Principal, renewed.Principal);
    }
}
