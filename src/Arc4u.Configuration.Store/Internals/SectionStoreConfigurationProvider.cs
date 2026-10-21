using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Store.Internals;

sealed class SectionStoreConfigurationProvider : ConfigurationProvider
{
    private readonly IReadOnlyCollection<(string Key, IValueHolder Value)> _initialData;
    private IServiceScopeFactory? _serviceScopeFactory;

    sealed class Transformer : JsonStreamConfigurationProvider
    {
        public Transformer()
            : base(new JsonStreamConfigurationSource(/* we don't set the Stream property since we call Load(Stream) directly*/))
        {
        }

        public void Transform(IDictionary<string, string?> output, string key, string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            Load(stream);

            const string ValuePropertyName = ValueHolder.ValuePropertyName;
            foreach (var item in Data)
            {
                if (item.Key == ValuePropertyName)
                {
                    // a simple type (no structure)
                    output[key] = item.Value;
                }
                else
                {
                    // a complex type
                    var p = item.Key.IndexOf(':');
                    if (p <= 0 || p != ValuePropertyName.Length || !item.Key.StartsWith(ValuePropertyName))
                    {
                        throw new InvalidOperationException($"The format of the key {item.Key} is not expected");
                    }
                    output[string.Concat(key, item.Key.AsSpan(p))] = item.Value;
                }
            }
        }
    }

    public SectionStoreConfigurationProvider(IReadOnlyCollection<(string Name, IValueHolder Value)> initialData)
    {
        _initialData = initialData;
    }

    private void InitializeSectionStore(ISectionStore sectionStore, List<SectionEntity> existingSections)
    {
        var existingKeys = existingSections.ToDictionary(section => section.Key, StringComparer.OrdinalIgnoreCase);
        var newSections = new List<SectionEntity>();
        foreach (var (Key, Value) in _initialData)
        {
            if (!existingKeys.ContainsKey(Key))
            {
                var entity = new SectionEntity { Key = Key };
                entity.SetValue(Value);
                newSections.Add(entity);
            }
        }

        if (newSections.Count > 0)
        {
            sectionStore.Add(newSections);
        }
    }

    private IDictionary<string, string?> GetData()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var transformer = new Transformer();

        if (_serviceScopeFactory is null)
        {
            foreach (var (Key, Value) in _initialData)
            {
                transformer.Transform(data, Key, Value.Serialize());
            }
        }
        else
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sectionStore = scope.ServiceProvider.GetRequiredService<ISectionStore>();
            var existingSections = sectionStore.GetAll();
            // The store is empty if someone reset it: re-seed it and read it back, so this poll publishes the initial data instead of an empty set.
            if (existingSections.Count == 0)
            {
                InitializeSectionStore(sectionStore, existingSections);
                existingSections = sectionStore.GetAll();
            }

            foreach (var entity in existingSections)
            {
                transformer.Transform(data, entity.Key, entity.Value);
            }
        }

        return data;
    }

    private static bool Equals(IDictionary<string, string?> data1, IDictionary<string, string?> data2)
    {
        if (data1.Count != data2.Count)
        {
            return false;
        }

        foreach (var item1 in data1)
        {
            if (!data2.TryGetValue(item1.Key, out var value2) || item1.Value != value2)
            {
                return false;
            }
        }

        return true;
    }

    public void Initialize(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        // Now that we have a service provider, initialize the store with the initial data if those entries were missing           
        using var scope = _serviceScopeFactory.CreateScope();
        var sectionStore = scope.ServiceProvider.GetRequiredService<ISectionStore>();
        var existingSections = sectionStore.GetAll();
        InitializeSectionStore(sectionStore, existingSections);
    }

    public void ReloadIfNeeded()
    {
        var data = GetData();
        if (Equals(data, Data))
        {
            return;
        }

        Data = data;
        OnReload();
    }

    public override void Load()
    {
        var data = GetData();
        Data = data;
        OnReload();
    }
}
