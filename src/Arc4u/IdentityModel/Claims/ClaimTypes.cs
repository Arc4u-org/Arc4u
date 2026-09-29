namespace Arc4u.IdentityModel.Claims;

/// <summary>
/// The claim types used by Arc4u. They complement <see cref="System.Security.Claims.ClaimTypes"/>.
/// </summary>
public class ClaimTypes
{
    /// <summary>
    /// Gets the claim type that holds the culture of the user (for example <c>en-GB</c>).
    /// </summary>
    public static string Culture { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/culture"; } }

    /// <summary>
    /// Gets the claim type that holds the authorization data (the JSON representation of an <see cref="Arc4u.Security.Principal.Authorization"/>).
    /// </summary>
    public static string Authorization { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/authorization"; } }

    /// <summary>
    /// Gets the claim type that holds the security identifier of a service.
    /// </summary>
    public static string ServiceSid { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/servicesid"; } }

    /// <summary>
    /// Gets the claim type that holds the company of the user.
    /// </summary>
    public static string Company { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/company"; } }

    /// <summary>
    /// Gets the claim type that holds the security identifier of the user.
    /// </summary>
    public static string Sid { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/sid"; } }

    /// <summary>
    /// Gets the claim type that holds the picture of the user.
    /// </summary>
    public static string UserPicture { get { return "http://schemas.arc4u.net/ws/2012/05/identity/claims/userPicture"; } }

    /// <summary>
    /// Gets the claim type (<c>name</c>) that holds the name of the user.
    /// </summary>
    public static string Name { get { return "name"; } }

    /// <summary>
    /// Gets the claim type (<c>family_name</c>) that holds the surname of the user.
    /// </summary>
    public static string Surname { get { return "family_name"; } }

    /// <summary>
    /// Gets the claim type (<c>given_name</c>) that holds the given name of the user.
    /// </summary>
    public static string GivenName { get { return "given_name"; } }

    /// <summary>
    /// Gets the claim type (<c>upn</c>) that holds the user principal name.
    /// </summary>
    public static string Upn { get { return "upn"; } }

    /// <summary>
    /// Gets the claim type (<c>email</c>) that holds the e-mail address of the user.
    /// </summary>
    public static string Email { get { return "email"; } }

    /// <summary>
    /// Gets the claim type (<c>primarysid</c>) that holds the primary security identifier.
    /// </summary>
    public static string PrimarySid { get { return "primarysid"; } }

    /// <summary>
    /// Gets the Microsoft identity platform claim type that holds the object identifier of the user.
    /// </summary>
    public static string ObjectIdentifier { get { return "http://schemas.microsoft.com/identity/claims/objectidentifier"; } }

    /// <summary>
    /// Gets the short claim type (<c>oid</c>) that holds the object identifier of the user.
    /// </summary>
    public static string OID { get { return "oid"; } }
}
