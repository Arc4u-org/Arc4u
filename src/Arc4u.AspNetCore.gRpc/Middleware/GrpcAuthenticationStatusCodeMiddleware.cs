using Microsoft.AspNetCore.Http;

namespace Arc4u.AspNetCore.Middleware;

/// <summary>
/// Middleware turning the redirect (HTTP 302) a gRPC request receives from an authentication challenge into <c>401 Unauthorized</c>.
/// </summary>
/// <remarks>A request is a gRPC request when its content type contains "grpc". When such a request ends with status 302 the response headers are cleared and the status set to 401. Add it with <see cref="GrpcAuthenticationStatusCodeMiddlewareExtension.AddGrpcAuthenticationControl(Microsoft.AspNetCore.Builder.IApplicationBuilder)"/>.</remarks>
public class GrpcAuthenticationStatusCodeMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next delegate of the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public GrpcAuthenticationStatusCodeMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Runs the rest of the pipeline, then replaces a 302 answer to a gRPC request by a 401.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>A task representing the processing of the request.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next.Invoke(context).ConfigureAwait(false);

        if (null != context.Request.ContentType &&
            null != context.Response &&
            context.Request.ContentType.Contains("grpc") &&
            context.Response.StatusCode == 302)
        {
            context.Response.Headers.Clear();
            context.Response.StatusCode = 401;
        }
    }
}
