namespace Arc4u.OAuth2.Net;

/// <summary>Builds the value of an HTTP <c>Authorization</c> header.</summary>
public interface IAuthorizationHeader
{
    /// <summary>Gets the value of the <c>Authorization</c> header.</summary>
    /// <param name="settings">The settings describing how the header is built.</param>
    /// <returns>The header value.</returns>
    string GetHeader(IKeyValueSettings settings);
}
