using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Extension methods to extract credentials from a Basic authentication payload.</summary>
public static class CredentialResultExtension
{
    /// <summary>
    /// Splits a <c>user:password</c> pair. When the user name is not in a recognized format (<c>user@domain.tld</c>, <c>DOMAIN\user</c> or <c>domain/user</c>),
    /// <paramref name="defaultUpn"/> is appended to it.
    /// </summary>
    /// <typeparam name="T">The type of the category of the logger.</typeparam>
    /// <param name="credentials">The credentials instance the method extends. It is not used.</param>
    /// <param name="pair">The decoded <c>user:password</c> pair.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="defaultUpn">The domain suffix (for example <c>@contoso.com</c>) appended to a user name without domain.</param>
    /// <returns>The credentials found, or a <see cref="CredentialsResult"/> with <see cref="CredentialsResult.CredentialsEntered"/> set to <see langword="false"/> when <paramref name="pair"/> has no <c>:</c> separator.</returns>
    public static CredentialsResult ExtractCredential<T>(this CredentialsResult credentials, [DisallowNull] string pair, ILogger<T> logger, string? defaultUpn = null)
    {
        var ix = pair.IndexOf(':');
        if (ix == -1)
        {
            logger.Technical().LogBasicAuthentityBadFormat();

            return new CredentialsResult(false);
        }

        var username = pair.Substring(0, ix);
        var pwd = pair.Substring(ix + 1);

        logger.Technical().LogUserName(username);

        // is username format ok?
        if (!Regex.IsMatch(username, @"([a-zA-Z0-9]+@[a-zA-Z0-9]+\.[a-zA-Z0-9]+)|([a-zA-Z0-9]+\\[a-zA-Z0-9]+)|([a-zA-Z0-9]+/[a-zA-Z0-9]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
        {
            var originalUserName = username;
            username = $"{username}{defaultUpn?.Trim()}";
            logger.Technical().LogChangeUserName(originalUserName, username);
        }

        return new CredentialsResult(true, username, pwd);
    }
}
