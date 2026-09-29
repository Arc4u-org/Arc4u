using System.Security.Claims;
using Arc4u.Dependency.Attribute;

namespace Arc4u.OAuth2.Security.Principal;

/// <summary>
/// This class is intented to be used in a service scenario where multiple users are connected.
/// The claims are cached based on a unique identifier from the claims in an identity.
/// </summary>
[Export(typeof(ICacheKeyGenerator)), Shared]
public class KeyGeneratorFromIdentity : ICacheKeyGenerator
{
    /// <summary>Initializes a new instance of the <see cref="KeyGeneratorFromIdentity"/> class.</summary>
    /// <param name="userKeyIdentifier">The service that finds the identifier of a user in an identity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="userKeyIdentifier"/> is <see langword="null"/>.</exception>
    public KeyGeneratorFromIdentity(IUserObjectIdentifier userKeyIdentifier)
    {
        _userKeyIdentifier = userKeyIdentifier ?? throw new ArgumentNullException(nameof(userKeyIdentifier));
    }

    private readonly IUserObjectIdentifier _userKeyIdentifier;

    /// <summary>Gets the cache key of the identity: the user identifier followed by <c>_ClaimsCache</c>.</summary>
    /// <param name="identity">The identity of the user.</param>
    /// <returns>The cache key.</returns>
    /// <exception cref="NullReferenceException">No user identifier is found in the identity.</exception>
    public string GetClaimsKey(ClaimsIdentity identity)
    {
        var id = _userKeyIdentifier.Getidentifier(identity);

        if (string.IsNullOrEmpty(id))
        {
            throw new NullReferenceException($"No distinguish key found for the identity {identity.Name}");
        }

        return id + "_ClaimsCache";
    }
}
