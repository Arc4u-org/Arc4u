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
    /// Initializes a new instance of the <see cref="ScopedServiceProviderAccessor"/> class.
    /// The http context accessor is never null on a service, but its <c>HttpContext</c> is null when the code does not run in a request, for example in a background job.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor of the current HTTP context.</param>
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
    /// <exception cref="NullReferenceException">Getter: no service provider was set for the current async flow and there is no current HTTP request.</exception>
    /// <exception cref="ArgumentNullException">Setter: the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Setter: the value is not an <see cref="IServiceScope"/>.</exception>
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
