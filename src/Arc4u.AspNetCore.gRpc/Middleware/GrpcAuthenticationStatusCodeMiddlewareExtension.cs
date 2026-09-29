using Microsoft.AspNetCore.Builder;

namespace Arc4u.AspNetCore.Middleware;

/// <summary>
/// Registration of <see cref="GrpcAuthenticationStatusCodeMiddleware"/> in the request pipeline.
/// </summary>
public static class GrpcAuthenticationStatusCodeMiddlewareExtension
{
    /// <summary>
    /// Adds the <see cref="GrpcAuthenticationStatusCodeMiddleware"/> to the pipeline, so a gRPC call rejected by an interactive
    /// authentication challenge (HTTP 302) answers <c>401 Unauthorized</c> instead of a redirect.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.AddGrpcAuthenticationControl();
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// </code>
    /// </example>
    public static IApplicationBuilder AddGrpcAuthenticationControl(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<GrpcAuthenticationStatusCodeMiddleware>();
    }
}
