namespace Arc4u.OAuth2.Options;
/// <summary>Protects one resource path with an authorization policy.</summary>
public class ValidateResourceRightMiddlewareOptions
{
    /// <summary>Gets or sets the name of the authorization policy the current principal must satisfy to access the resource.</summary>
    public string AuthorizationPolicy { get; set; } = default!;

    /// <summary>Gets or sets the request path of the resource. The comparison ignores the case and the leading and trailing slashes.</summary>
    public string Path { get; set; } = default!;

    /// <summary>
    /// Gets or sets the content written when access is denied. When empty, <see cref="ValidateResourcesRightMiddlewareOptions.DefaultContent"/> is used.
    /// </summary>
    public string ContentToDisplay { get; set; } = default!;

}
