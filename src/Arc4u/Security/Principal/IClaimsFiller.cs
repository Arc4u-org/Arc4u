using System.Security.Principal;
using Arc4u.IdentityModel.Claims;

namespace Arc4u.Security.Principal;

/// <summary>
/// Provides additional claims for an identity, for example from a back-end service.
/// </summary>
public interface IClaimsFiller
{
    /// <summary>
    /// Gets the claims of an identity.
    /// </summary>
    /// <param name="identity">The identity for which claims are requested.</param>
    /// <returns>The claims.</returns>
    Task<IEnumerable<ClaimDto>> GetAsync(IIdentity identity);
}
