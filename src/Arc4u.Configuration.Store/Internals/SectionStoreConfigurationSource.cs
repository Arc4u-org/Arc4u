using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Store.Internals;

sealed class SectionStoreConfigurationSource : IConfigurationSource
{
    private readonly SectionStoreConfigurationOptions _options;

    public SectionStoreConfigurationSource(SectionStoreConfigurationOptions options) => _options = options;

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new SectionStoreConfigurationProvider(_options.GetInitialData(builder, this));
}
