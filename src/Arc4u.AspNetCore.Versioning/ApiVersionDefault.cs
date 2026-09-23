using Asp.Versioning;

namespace Arc4u.AspNetCore.Versioning;

public class ApiVersionDefault
{
    /// <summary>
    /// The first version of the interface api.
    /// </summary>
    public static readonly ApiVersion V1 = new ApiVersion(1);
}
