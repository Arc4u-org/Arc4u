using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// The keys of the keyed <see cref="ITokenProvider"/> registrations that can hold a user session, read from the service collection the first time they are needed
/// (the collection is complete once the container is built, so the order of the registrations does not matter).
/// It is registered by <see cref="SignOutTokenProviderExtensions.AddTokenProvidersSignOut"/>.
/// </summary>
/// <param name="services">The service collection of the application.</param>
public sealed class SignOutTokenProviderKeys(IServiceCollection services)
{
    // The collection is only captured by the factory, which Lazy releases once the keys are computed: it is not kept for the lifetime of the application.
    // PublicationOnly does not cache an exception of the factory: a failed computation is retried by the next sign-out.
    private readonly Lazy<IReadOnlyList<object>> _keys = new(() => services.Where(IsSignOutCandidate)
                                                                            .Select(d => d.ServiceKey!)
                                                                            .Distinct()
                                                                            .ToList(),
                                                             LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Gets the keys of the token providers to sign out.</summary>
    public IReadOnlyList<object> Keys => _keys.Value;

    // A provider registered with a factory has an unknown type: it is resolved and checked at sign-out.
    private static bool IsSignOutCandidate(ServiceDescriptor descriptor)
        => descriptor.IsKeyedService
           && descriptor.ServiceType == typeof(ITokenProvider)
           && descriptor.ServiceKey is not null
           && !ReferenceEquals(descriptor.ServiceKey, KeyedService.AnyKey)
           && (descriptor.KeyedImplementationType is not null ? typeof(ISignOutTokenProvider).IsAssignableFrom(descriptor.KeyedImplementationType)
               : descriptor.KeyedImplementationInstance is not null ? descriptor.KeyedImplementationInstance is ISignOutTokenProvider
               : true);
}

/// <summary>Signs the user out of the token providers that hold a user session.</summary>
public static class SignOutTokenProviderExtensions
{
    /// <summary>
    /// Registers <see cref="SignOutTokenProviderKeys"/>, so <see cref="SignOutTokenProvidersAsync"/> resolves each token provider separately:
    /// a provider that cannot be resolved (missing or scoped dependency) does not prevent the sign-out of the others. It can be called before or after the token providers are registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTokenProvidersSignOut(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(new SignOutTokenProviderKeys(services));

        return services;
    }

    /// <summary>
    /// Calls <see cref="ISignOutTokenProvider.SignOutAsync"/> on every keyed <see cref="ITokenProvider"/> of the container that implements <see cref="ISignOutTokenProvider"/>,
    /// once per instance. The same settings are given to every provider: a provider ignores the settings that do not designate it.
    /// When <see cref="SignOutTokenProviderKeys"/> is registered (<see cref="AddTokenProvidersSignOut"/>), each provider is resolved separately; otherwise all the keyed providers are resolved together,
    /// and one that cannot be resolved prevents the sign-out of all of them.
    /// An exception thrown when the providers are resolved or signed out is logged and not rethrown, so one provider cannot prevent the sign-out of the others.
    /// </summary>
    /// <typeparam name="T">The category of the logger.</typeparam>
    /// <param name="container">The service provider in which the token providers are registered.</param>
    /// <param name="settings">The settings given to each provider.</param>
    /// <param name="logger">The logger in which the exceptions are written.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when every provider is signed out.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is cancelled.</exception>
    public static async ValueTask SignOutTokenProvidersAsync<T>(this IServiceProvider container, IKeyValueSettings settings, ILogger<T> logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(settings);

        // A provider registered under several keys (or as a singleton) is signed out once.
        var providers = new List<ISignOutTokenProvider>();
        IReadOnlyList<object>? keys = null;
        try
        {
            keys = container.GetService<SignOutTokenProviderKeys>()?.Keys;
        }
        catch (Exception ex)
        {
            // The keys cannot be computed: all the keyed providers are resolved together below.
            LogException(logger, ex);
        }

        if (keys is not null)
        {
            foreach (var key in keys)
            {
                try
                {
                    if (container.GetKeyedService<ITokenProvider>(key) is ISignOutTokenProvider provider && !providers.Contains(provider, ReferenceEqualityComparer.Instance))
                    {
                        providers.Add(provider);
                    }
                }
                catch (Exception ex)
                {
                    LogException(logger, ex);
                }
            }
        }
        else
        {
            try
            {
                providers.AddRange(container.GetKeyedServices<ITokenProvider>(KeyedService.AnyKey)
                                            .OfType<ISignOutTokenProvider>()
                                            .Distinct<ISignOutTokenProvider>(ReferenceEqualityComparer.Instance));
            }
            catch (Exception ex)
            {
                LogException(logger, ex);
                return;
            }
        }

        foreach (var provider in providers)
        {
            try
            {
                await provider.SignOutAsync(settings, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogException(logger, ex);
            }
        }
    }

    // Logging never interrupts the sign-out, whatever the logger registered by the application.
    private static void LogException<T>(ILogger<T> logger, Exception ex) => logger.LogTechnicalException(ex, "A token provider cannot be signed out.");
}
