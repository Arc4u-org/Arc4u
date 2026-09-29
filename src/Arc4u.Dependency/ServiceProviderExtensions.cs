#if NET8_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Dependency;
/// <summary>
/// Extension methods on <see cref="IServiceProvider"/> to resolve services without throwing when they cannot be created.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Tries to resolve a service of type <typeparamref name="T"/>. Any exception raised while resolving is swallowed.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="service">The resolved service, or <see langword="null"/> when it is not registered or cannot be created.</param>
    /// <returns><see langword="true"/> when a service was resolved; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code language="csharp">
    /// if (serviceProvider.TryGetService&lt;IMyOptionalService&gt;(out var service))
    /// {
    ///     service!.DoWork();
    /// }
    /// </code>
    /// </example>
    public static bool TryGetService<T>(this IServiceProvider provider, out T? service)
    {
        try
        {
            service = provider.GetService<T>();
        }
        catch (Exception)
        {
            service = default;
        }
        return null != service;
    }

    /// <summary>
    /// Tries to resolve a keyed service of the given type. Any exception raised while resolving (including a missing registration) is swallowed.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="type">The type of the service.</param>
    /// <param name="name">The key under which the service is registered.</param>
    /// <param name="value">The resolved service, or <see langword="null"/> when it cannot be resolved.</param>
    /// <returns><see langword="true"/> when a service was resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryGetService(this IServiceProvider provider, Type type, string name, out object? value)
    {
        try
        {
            value = provider.GetRequiredKeyedService(type, name);
            return value is not null;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to resolve a keyed service of type <typeparamref name="T"/>. Any exception raised while resolving is swallowed.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="name">The key under which the service is registered.</param>
    /// <param name="service">The resolved service, or <see langword="null"/> when it is not registered or cannot be created.</param>
    /// <returns><see langword="true"/> when a service was resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryGetService<T>(this IServiceProvider provider, string name, out T? service)
    {
        try
        {
            service = provider.GetKeyedService<T>(name);

        }
        catch (Exception)
        {
            service = default;
        }
        return null != service;
    }

}

#endif
