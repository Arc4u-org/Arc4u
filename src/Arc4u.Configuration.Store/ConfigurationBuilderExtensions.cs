using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Store;

using Internals;

/// <summary>
/// Extension methods on <see cref="IConfigurationBuilder"/> to persist configuration sections in a store.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Define which sections need to be persisted. The sections declared here are created in the store, with the given initial values,
    /// when they do not exist yet; afterwards the values read from the store are exposed as configuration.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configure">An action declaring the sections to persist.</param>
    /// <returns>The configuration builder, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// builder.Configuration.AddSectionStoreConfiguration(options =&gt; options
    ///     .Add("Features", new FeatureOptions { Enabled = true })
    ///     .Add&lt;LoggingOptions&gt;("Logging"));
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddSectionStoreConfiguration(this IConfigurationBuilder builder, Action<ISectionStoreConfigurationOptions> configure)
    {
        var options = new SectionStoreConfigurationOptions();
        configure(options);
        return builder.Add(new SectionStoreConfigurationSource(options));
    }
}
