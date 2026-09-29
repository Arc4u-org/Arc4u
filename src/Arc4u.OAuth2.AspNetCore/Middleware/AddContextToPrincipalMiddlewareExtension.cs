using Microsoft.AspNetCore.Builder;

namespace Arc4u.OAuth2.Middleware;
/// <summary>Registers the <see cref="AddContextToPrincipalMiddleware"/>.</summary>
public static class AddContextToPrincipalMiddlewareExtension
{
    /// <summary>Adds the <see cref="AddContextToPrincipalMiddleware"/> to the request pipeline. Call it after the authentication middleware.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// app.UseAuthentication();
    /// app.UseAddContextToPrincipal();
    /// app.UseAuthorization();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseAddContextToPrincipal(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<AddContextToPrincipalMiddleware>();
    }
}
