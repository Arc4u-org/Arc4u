
namespace Arc4u.OAuth2.Options;
/// <summary>Configures the resources right validation middleware.</summary>
public class ValidateResourcesRightMiddlewareOptions
{
    /// <summary>Gets or sets the content returned when access is denied and the resource has no <see cref="ValidateResourceRightMiddlewareOptions.ContentToDisplay"/>. The default is an empty Swagger 2.0 document titled "You are not authorized!".</summary>
    public string DefaultContent { get; set; } = "{\"swagger\": \"2.0\",  \"info\": { \"title\": \"You are not authorized!\", \"version\": \"1.0.0\" }, \"consumes\": [ \"application/json\"  ],  \"produces\": [ \"application/json\" ]}";

    /// <summary>
    /// Gets or sets the protected resources. The key is only a description of the entry: it is not used, but it must not be empty.
    /// </summary>
    public Dictionary<string, ValidateResourceRightMiddlewareOptions> ResourcesPolicies { get; set; } = default!;
}
