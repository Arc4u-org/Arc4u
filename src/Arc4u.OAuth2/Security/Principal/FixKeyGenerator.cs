using System.Security.Claims;
using Arc4u.Dependency.Attribute;

namespace Arc4u.OAuth2.Security.Principal;

/// <summary>
/// This class is used with UI application which is user based already (by design).
/// So the key to used can be simple as a fix string.
/// </summary>
[Export(typeof(ICacheKeyGenerator)), Shared]
public class FixKeyGenerator : ICacheKeyGenerator
{
    /// <summary>Gets the fixed cache key <c>ClaimsVaultRef</c>, whatever the identity.</summary>
    /// <param name="identity">The identity, which is ignored.</param>
    /// <returns>The constant key <c>ClaimsVaultRef</c>.</returns>
    public string GetClaimsKey(ClaimsIdentity identity)
    {
        return "ClaimsVaultRef";
    }
}
