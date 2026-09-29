#if NET8_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Dependency;

/// <summary>
/// Extension methods on <see cref="IServiceCollection"/> to inspect the registered services.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Extract from the service collection the implementation type of the interface T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <returns>Null or the type.</returns>
    public static Type? GetImplementationType<T>(this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(descriptor =>
                                                    descriptor.ServiceType == typeof(T) ||
                                                    (descriptor.ImplementationType?.IsSubclassOf(typeof(T)) ?? false));

        return serviceDescriptor?.ImplementationType;
    }
}
#endif
