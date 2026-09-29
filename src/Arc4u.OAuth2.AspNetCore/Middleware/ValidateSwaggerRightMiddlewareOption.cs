namespace Arc4u.OAuth2.Middleware;

/// <summary>Configures the <see cref="ValidateSwaggerRightMiddleware"/>.</summary>
public class ValidateSwaggerRightMiddlewareOption
{
    /// <summary>Gets or sets the identifier of the right (operation) that the principal must have, checked with <c>IsAuthorized</c>.</summary>
    public int Access { get; set; }

    /// <summary>Gets or sets the request path of the Swagger document to protect. The comparison ignores the case and the leading and trailing slashes.</summary>
    public string Path { get; set; } = default!;

    /// <summary>Gets or sets the content written when access is denied. The default is an empty Swagger 2.0 document titled "You are not authorized!".</summary>
    public string ContentToDisplay { get; set; } = "{\"swagger\": \"2.0\",  \"info\": { \"title\": \"You are not authorized!\", \"version\": \"1.0.0\" }, \"consumes\": [ \"application/json\"  ],  \"produces\": [ \"application/json\" ]}";
}
