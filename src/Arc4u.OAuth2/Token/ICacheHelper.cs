using Arc4u.Caching;

namespace Arc4u.OAuth2.Token;
/// <summary>Gives access to the cache used to store the tokens.</summary>
public interface ICacheHelper
{
    /// <summary>Gets the cache used to store the tokens.</summary>
    /// <returns>The token cache.</returns>
    ICache GetCache();
}