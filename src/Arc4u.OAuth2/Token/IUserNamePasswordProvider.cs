using Arc4u.OAuth2.Security.Principal;

namespace Arc4u.OAuth2.Token;

/// <summary>Validates a user name and a password.</summary>
/// <param name="upn">The user principal name.</param>
/// <param name="password">The password.</param>
/// <returns><see langword="true"/> when the credentials are valid.</returns>
public delegate Task<bool> CheckCredentialsAsync(string upn, string password);

/// <summary>Provides the user name and password of the user, for example by prompting him.</summary>
public interface IUserNamePasswordProvider
{
    /// <summary>
    /// Used to provide the user name and password.
    /// </summary>
    /// <param name="upn">The current know upn of the user. Can be null if unknown.</param>
    /// <param name="checkCredentials"></param>
    /// <returns>The <see cref="CredentialsResult"/> containing the upn and password of the user.</returns>
    Task<CredentialsResult> GetCredentials(string? upn, CheckCredentialsAsync checkCredentials);
}
