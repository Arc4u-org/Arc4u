using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.AspNetCore.Versioning;

/// <summary>
/// Registers the api versioning used by the interface endpoints.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// The header used to give the api version.
    /// </summary>
    public const string VersionHeader = "x-api-version";

    /// <summary>
    /// The query string parameter used to give the api version.
    /// </summary>
    public const string VersionQueryString = "api-version";

    /// <summary>
    /// Adds api versioning where the version is read from the url segment (v1), the <see cref="VersionHeader"/> header
    /// or the <see cref="VersionQueryString"/> query string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the api versioning to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInterfaceApiVersioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = ApiVersionDefault.V1;
                // A version is mandatory: no implicit fallback when the caller does not give one.
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader(VersionHeader),
                    new QueryStringApiVersionReader(VersionQueryString));
            })
            .AddApiExplorer(options =>
            {
                // Documents interface/v1/Environment instead of interface/v{version}/Environment.
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}

