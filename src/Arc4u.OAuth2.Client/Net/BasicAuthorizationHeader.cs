using System.Text;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;

namespace Arc4u.OAuth2.Net;

/// <summary>Builds a Basic <c>Authorization</c> header from the user name and password kept in the secure cache.</summary>
[Export(typeof(IBasicAuthorizationHeader)), Shared]
public class BasicAuthorizationHeader : IBasicAuthorizationHeader
{
    /// <summary>Initializes a new instance of the <see cref="BasicAuthorizationHeader"/> class.</summary>
    /// <param name="secureCache">The secure cache holding the user name and the password.</param>
    public BasicAuthorizationHeader(ISecureCache secureCache)
    {
        _secureCache = secureCache;
    }

    private readonly ISecureCache _secureCache;

    /// <summary>
    /// Gets the header value <c>Basic base64(upn:password)</c>. The user name and password are read from the secure cache under the keys <c>{store key}_upn</c> and <c>{store key}_pwd</c>, where the store key is
    /// the <see cref="TokenKeys.PasswordStoreKey"/> value of the settings (<c>secret</c> when it is missing or empty).
    /// </summary>
    /// <param name="settings">The settings, optionally giving the store key.</param>
    /// <returns>The header value.</returns>
    /// <exception cref="InvalidOperationException">The user name or the password is not in the cache.</exception>
    public string GetHeader(IKeyValueSettings settings)
    {
        ExtractFromSettings(settings, out var passwordStoreKey);

        var userkey = passwordStoreKey + "_upn";
        var pwdkey = passwordStoreKey + "_pwd";

        _secureCache.TryGetValue<string>(userkey, out var upn);
        _secureCache.TryGetValue<string>(pwdkey, out var pwd);

        if (string.IsNullOrWhiteSpace(upn) || string.IsNullOrWhiteSpace(pwd))
        {
            throw new InvalidOperationException("The upn or password is not set in the cache!");
        }

        return $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{upn!.Trim()}:{pwd!.Trim()}"))}";
    }

    private static void ExtractFromSettings(IKeyValueSettings settings, out string passwordStoreKey)
    {
        passwordStoreKey = "secret";
        if (settings.Values.ContainsKey(TokenKeys.PasswordStoreKey))
        {
            passwordStoreKey = string.IsNullOrWhiteSpace(settings.Values[TokenKeys.PasswordStoreKey]) ? passwordStoreKey : settings.Values[TokenKeys.PasswordStoreKey];
        }

    }
}
