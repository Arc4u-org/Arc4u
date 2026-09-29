using Arc4u.Dependency.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.AspNetCore;

/// <summary>Gives access to the <see cref="IServiceProvider"/> of the current scope: the one set explicitly for the current async flow, otherwise the one of the current HTTP request.</summary>
[Export(typeof(IScopedServiceProviderAccessor))]
[Shared]
public class ScopedServiceProviderAccessor : IScopedServiceProviderAccessor
{
    /// <summary>
    /// See https://stackoverflow.com/questions/72313355/why-is-the-httpcontextholder-needed-when-implementing-the-httpcontextaccessor-ba for an explanation why we are not storing the value directly.
    /// </summary>
    private sealed class ServiceProviderHolder
    {
        public IServiceProvider? ServiceProvider;
    }

    private static readonly AsyncLocal<ServiceProviderHolder> _serviceProviderCurrent = new();
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Will inject the http context accessor. This is nerver null on a service but the http context can be null if the context is not created from a request context like gRPC or http.
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public ScopedServiceProviderAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Clears the service provider set for the current async flow, for example when its scope is disposed.</summary>
    public void InvalidateServiceProvider()
    {
        var holder = _serviceProviderCurrent.Value;
        // Clear current service provider trapped in the AsyncLocals, as it's done.
        if (holder != null)
        {
            holder.ServiceProvider = null;
        }
    }

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider
    {
        get
        {
            // the current has precedence over the http context accessor,
            // since the current might become invalid when the scope is disposed without us knowing it ???
            var value = _serviceProviderCurrent.Value?.ServiceProvider ?? _httpContextAccessor?.HttpContext?.RequestServices;
            if (value is null)
            {
                throw new NullReferenceException(nameof(value));
            }

            return value;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value is not IServiceScope)
            {
                throw new ArgumentException("The ServiceProvider must be a scoped one!");
            }

            InvalidateServiceProvider();

            _serviceProviderCurrent.Value = new ServiceProviderHolder { ServiceProvider = value };
        }
    }
}
