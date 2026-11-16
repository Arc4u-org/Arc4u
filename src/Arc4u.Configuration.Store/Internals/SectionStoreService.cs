using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Store.Internals;

sealed class SectionStoreService : ISectionStoreService
{
    private readonly IConfiguration _configuration;
    private readonly Lock _lock;
    private bool _startupCalled;

    public SectionStoreService(IConfiguration configuration)
    {
        _configuration = configuration;
        _lock = new();
    }

    private IEnumerable<SectionStoreConfigurationProvider> EnumerateSectionStoreConfigurationProviders()
    {
        if (_configuration is not IConfigurationRoot configurationRoot)
        {
            throw new InvalidOperationException($"Expected {typeof(IConfigurationRoot).Name} but got {_configuration.GetType().Name}");
        }

        foreach (var provider in configurationRoot.Providers)
        {
            if (provider is SectionStoreConfigurationProvider sectionStoreConfigurationProvider)
            {
                yield return sectionStoreConfigurationProvider;
            }
        }
    }

    public void Startup(IServiceScopeFactory serviceScopeFactory)
    {
        foreach (var sectionStoreConfigurationProvider in EnumerateSectionStoreConfigurationProviders())
        {
            sectionStoreConfigurationProvider.Initialize(serviceScopeFactory);
        }

        lock (_lock)
        {
            _startupCalled = true;
        }
    }

    public void ReloadIfNeeded()
    {
        lock (_lock)
        {
            if (!_startupCalled)
            {
                return;
            }
        }

        foreach (var sectionStoreConfigurationProvider in EnumerateSectionStoreConfigurationProviders())
        {
            sectionStoreConfigurationProvider.ReloadIfNeeded();
        }
    }
}
