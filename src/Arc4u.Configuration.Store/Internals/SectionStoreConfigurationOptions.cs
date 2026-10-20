using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Store.Internals;

sealed class SectionStoreConfigurationOptions : ISectionStoreConfigurationOptions
{
    private readonly List<(string Key, IValueHolder Value)> _objects;
    private readonly List<IValueBuilder> _sections;

    private interface IValueBuilder
    {
        string Key { get; }
        IValueHolder Build(IConfiguration configuration);
    }

    private sealed class ValueBuilder<TValue>(string key, Func<IConfigurationSection, TValue?> bind) : IValueBuilder
    {
        public string Key { get; } = key;

        public IValueHolder Build(IConfiguration configuration) => ValueHolder.Create(bind(configuration.GetRequiredSection(Key)));
    }

    public SectionStoreConfigurationOptions()
    {
        _objects = new();
        _sections = new();
    }

    private static void CheckKeyArgument(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "The key of a section cannot be null");
        }
    }

    public ISectionStoreConfigurationOptions Add<TValue>(string key, TValue? value)
    {
        CheckKeyArgument(key);
        _objects.Add((key, ValueHolder.Create(value)));
        return this;
    }

    [RequiresUnreferencedCode("The section is bound with reflection. Use AddFromConfiguration with a binding function, e.g. s => s.Get<MyOptions>(), so the binding is generated in your application.")]
    [RequiresDynamicCode("The section is bound with reflection. Use AddFromConfiguration with a binding function, e.g. s => s.Get<MyOptions>(), so the binding is generated in your application.")]
    public ISectionStoreConfigurationOptions Add<TValue>(string key)
    {
        // TValue is only known by the application, so the reflection binder is used (the binding generator is
        // disabled in this project): the attributes above report it to trimmed and AOT applications.
        return AddFromConfiguration(key, section => section.Get<TValue>());
    }

    public ISectionStoreConfigurationOptions AddFromConfiguration<TValue>(string key, Func<IConfigurationSection, TValue?> bind)
    {
        CheckKeyArgument(key);
        ArgumentNullException.ThrowIfNull(bind);
        _sections.Add(new ValueBuilder<TValue>(key, bind));
        return this;
    }

    public IReadOnlyList<(string Key, IValueHolder Value)> GetInitialData(IConfigurationBuilder builder, IConfigurationSource self)
    {
        if (_sections.Count == 0)
        {
            return _objects;
        }
        else
        {
            var data = new List<(string Key, IValueHolder Value)>(_objects);
            var configurationRoot = BuildPreviousSources(builder, self);
            foreach (var valueBuilder in _sections)
            {
                data.Add((valueBuilder.Key, valueBuilder.Build(configurationRoot)));
            }

            return data;
        }
    }

    // The sections are read from the providers registered before the section store.
    // A ConfigurationManager (WebApplicationBuilder.Configuration) already holds exactly those providers.
    // A ConfigurationBuilder must not be built as a whole: that would build this source again, endlessly.
    private static IConfiguration BuildPreviousSources(IConfigurationBuilder builder, IConfigurationSource self)
    {
        if (builder is IConfiguration configuration)
        {
            return configuration;
        }

        var previous = new ConfigurationBuilder();
        foreach (var property in builder.Properties)
        {
            previous.Properties[property.Key] = property.Value;
        }

        foreach (var source in builder.Sources.TakeWhile(source => !ReferenceEquals(source, self)))
        {
            previous.Add(source);
        }

        return previous.Build();
    }
}
