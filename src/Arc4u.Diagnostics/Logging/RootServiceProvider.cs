namespace Arc4u.Diagnostics;

/// <summary>
/// Holds the root <see cref="IServiceProvider"/>. Registered as a singleton, it is always created from the root, so a service that compares the
/// <see cref="IServiceProvider"/> injected in it with <see cref="Provider"/> knows whether it was resolved in a scope.
/// </summary>
/// <remarks><c>provider is IServiceScope</c> cannot be used for this: the root provider given to the services also implements <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/>.</remarks>
internal sealed class RootServiceProvider(IServiceProvider provider)
{
    public IServiceProvider Provider { get; } = provider;

    /// <summary>Returns <see langword="true"/> when <paramref name="serviceProvider"/> is the provider of a scope, not the root one.</summary>
    public bool IsScope(IServiceProvider serviceProvider) => !ReferenceEquals(serviceProvider, Provider);
}
