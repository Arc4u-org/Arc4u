using Asp.Versioning;

namespace Arc4u.AspNetCore.Versioning;

/// <summary>
/// Well-known <see cref="ApiVersion"/> values of the Arc4u interface endpoints.
/// </summary>
public static class ApiVersionDefault
{
    /// <summary>
    /// The first version of the interface api.
    /// </summary>
    public static readonly ApiVersion V1 = new ApiVersion(1);
}
