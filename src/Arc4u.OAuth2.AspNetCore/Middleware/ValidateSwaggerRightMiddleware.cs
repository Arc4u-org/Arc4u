using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Middleware;

/// <summary>
/// A middleware that protects the Swagger document path: when the current principal does not have the right <see cref="ValidateSwaggerRightMiddlewareOption.Access"/>,
/// the response is a status 200 with <see cref="ValidateSwaggerRightMiddlewareOption.ContentToDisplay"/> instead of the document.
/// </summary>
public class ValidateSwaggerRightMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ValidateSwaggerRightMiddlewareOption _option;

    /// <summary>Initializes a new instance of the <see cref="ValidateSwaggerRightMiddleware"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="option">The path to protect and the right to check.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public ValidateSwaggerRightMiddleware(RequestDelegate next, ValidateSwaggerRightMiddlewareOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        _next = next ?? throw new ArgumentNullException(nameof(next));

        _option = option;
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is written or the rest of the pipeline is done.</returns>
    public async Task Invoke(HttpContext context)
    {
        var applicationContext = context.RequestServices.GetRequiredService<IApplicationContext>();

        if (context.Request.Path.HasValue &&
            string.Equals(context.Request.Path.Value.Trim('/'), _option.Path.Trim('/'), StringComparison.OrdinalIgnoreCase) &&
            applicationContext.Principal is not null)
        {
            if (applicationContext.Principal.IsAuthorized(_option.Access))
            {
                await _next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(_option.ContentToDisplay).ConfigureAwait(false);
            }
        }
        else
        {
            await _next.Invoke(context).ConfigureAwait(false);
        }
    }

}
