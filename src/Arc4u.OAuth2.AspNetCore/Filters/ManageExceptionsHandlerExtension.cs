using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>Registers the <see cref="ManageExceptionsHandler"/>.</summary>
public static class ManageExceptionsHandlerExtension
{
    /// <summary>
    /// Registers <see cref="ManageExceptionsHandler"/> as an <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/>, and the problem details services
    /// that <c>app.UseExceptionHandler()</c> requires when it is called without a path or a handler.
    /// Add <c>app.UseExceptionHandler()</c> to the pipeline so it is called.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddManageExceptionsHandler();
    /// ...
    /// app.UseExceptionHandler();
    /// </code>
    /// </example>
    public static IServiceCollection AddManageExceptionsHandler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Without it, the parameterless UseExceptionHandler() throws at startup.
        services.AddProblemDetails();

        return services.AddExceptionHandler<ManageExceptionsHandler>();
    }
}
