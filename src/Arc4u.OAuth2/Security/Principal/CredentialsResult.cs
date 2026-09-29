namespace Arc4u.OAuth2.Security.Principal;

/// <summary>
/// Return the Upn and Password implemented with the IUsernamePasswordProvider.
/// </summary>
public class CredentialsResult
{
    /// <summary>Gets a value indicating whether the user entered credentials.</summary>
    public bool CredentialsEntered { get; }
    /// <summary>Gets the user principal name entered by the user.</summary>
    public string? Upn { get; }
    /// <summary>Gets the password entered by the user.</summary>
    public string? Password { get; }
    /// <summary>Initializes a new instance of the <see cref="CredentialsResult"/> class.</summary>
    /// <param name="credentialsEntered"><see langword="true"/> when the user entered credentials.</param>
    /// <param name="upn">The user principal name.</param>
    /// <param name="passwd">The password.</param>
    public CredentialsResult(bool credentialsEntered, string? upn = null, string? passwd = null)
    {
        CredentialsEntered = credentialsEntered;
        Upn = upn;
        Password = passwd;
    }
}
