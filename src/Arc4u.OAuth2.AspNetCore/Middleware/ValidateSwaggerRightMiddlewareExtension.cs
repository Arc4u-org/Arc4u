using Microsoft.AspNetCore.Builder;

namespace Arc4u.OAuth2.Middleware;

/// <summary>Adds the <see cref="ValidateSwaggerRightMiddleware"/>.</summary>
public static class ValidateSwaggerRightMiddlewareExtension
{
    /// <summary>Adds the <see cref="ValidateSwaggerRightMiddleware"/> to the request pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <param name="option">The path to protect and the right to check.</param>
    /// <returns>The application builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IApplicationBuilder AddValidateSwaggerRightFor(this IApplicationBuilder app, ValidateSwaggerRightMiddlewareOption option)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(option);

        return app.UseMiddleware<ValidateSwaggerRightMiddleware>(option);
    }
}
