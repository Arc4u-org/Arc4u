using System.Security.Claims;

namespace Arc4u.OAuth2.Security;

/// <summary>Finds the identifier of a user in the claims of an identity.</summary>
public interface IUserObjectIdentifier
{
    /// <summary>Gets the identifier of the user.</summary>
    /// <param name="identity">The identity of the user.</param>
    /// <returns>The value of the first claim whose type is configured to identify a user, or <see langword="null"/> when there is none.</returns>
    public string? Getidentifier(ClaimsIdentity identity);
}
