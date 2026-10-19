using Microsoft.AspNetCore.Http;

namespace Arc4u.OAuth2.Middleware;

/// <summary>Matches a request path with the path of a resource protected by a middleware.</summary>
internal static class ProtectedPath
{
    /// <summary>
    /// Gets whether the request targets the protected path. The comparison ignores the case and the leading and trailing slashes.
    /// When the protected path has an extension, the same path with another extension matches too: Swashbuckle serves its document
    /// for <c>swagger.yaml</c> and <c>swagger.yml</c>, and up to 6.5 for any extension (<c>swagger.jsonx</c>), so checking only
    /// <c>swagger.json</c> lets the document through.
    /// </summary>
    /// <param name="requestPath">The path of the request.</param>
    /// <param name="protectedPath">The path of the protected resource.</param>
    /// <returns><see langword="true"/> when the request targets the protected resource.</returns>
    public static bool Matches(PathString requestPath, string protectedPath)
    {
        if (!requestPath.HasValue)
        {
            return false;
        }

        var request = requestPath.Value.Trim('/');
        var resource = protectedPath.Trim('/');

        if (string.Equals(request, resource, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = resource.LastIndexOf('.');
        if (extension <= resource.LastIndexOf('/') + 1)
        {
            return false;
        }

        // The resource without its extension, the dot included: "swagger/v1/swagger.".
        var stem = resource[..(extension + 1)];

        return request.Length > stem.Length &&
               request.StartsWith(stem, StringComparison.OrdinalIgnoreCase) &&
               request.IndexOf('/', stem.Length) < 0;
    }
}
