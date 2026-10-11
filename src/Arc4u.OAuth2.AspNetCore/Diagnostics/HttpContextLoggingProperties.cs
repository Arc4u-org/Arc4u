using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.AspNetCore.Diagnostics;

/// <summary>
/// An <see cref="IAddPropertiesToLog"/> for the <c>"Transient"</c> key, used by the loggers resolved from the root (the loggers of singletons).
/// It has no scoped dependency: each time an entry is written, it adds the properties of the <c>"Scoped"</c> provider of the current HTTP request
/// (for example <see cref="DefaultLoggingProperties"/>). Outside a request it adds nothing.
/// </summary>
public sealed class HttpContextLoggingProperties : IAddPropertiesToLog
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="HttpContextLoggingProperties"/> class.</summary>
    /// <param name="httpContextAccessor">The accessor of the current HTTP context.</param>
    public HttpContextLoggingProperties(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public IDictionary<string, object> GetProperties()
    {
        IAddPropertiesToLog? extraPropertiesToLog;
        try
        {
            // Add the Scoped properties.
            extraPropertiesToLog = _httpContextAccessor.HttpContext?.RequestServices?.GetKeyedService<IAddPropertiesToLog>("Scoped");
        }
        catch (ObjectDisposedException)
        {
            // The request scope is already disposed: an entry written after the end of the request.
            extraPropertiesToLog = null;
        }

        return extraPropertiesToLog is null ? new Dictionary<string, object>() : extraPropertiesToLog.GetProperties();
    }
}
