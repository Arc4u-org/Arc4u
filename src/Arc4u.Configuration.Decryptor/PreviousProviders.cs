using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Decryptor;

/// <summary>
/// The providers defined before a decryptor source, resolved without building their sources a second time when possible.
/// </summary>
/// <remarks>
/// A source can only be built and loaded once when it wraps a stream (<c>AddJsonStream</c>), and a remote source
/// (Azure Key Vault, ...) is called once more for each extra build. So the decryptor reads the providers already built:
/// <list type="bullet">
/// <item>with a <see cref="ConfigurationManager"/>, the providers of the earlier sources are built and loaded when the decryptor is added;</item>
/// <item>with a plain <see cref="ConfigurationBuilder"/>, the extension methods wrap the earlier sources in a <see cref="ProviderCapturingSource"/>
/// which keeps the provider the builder creates.</item>
/// </list>
/// A source that cannot be resolved this way (decryptor added with <see cref="IConfigurationBuilder.Add"/>, or a <see cref="ConfigurationManager"/>
/// rebuilding all its sources after one was inserted or removed) is built again, as before.
/// </remarks>
internal sealed class PreviousProviders
{
    private PreviousProviders(IReadOnlyList<(IConfigurationProvider Provider, bool MustLoad)> providers)
    {
        _providers = providers;
    }

    private readonly IReadOnlyList<(IConfigurationProvider Provider, bool MustLoad)> _providers;

    /// <summary>
    /// Resolve the providers of the sources defined before <paramref name="decryptorSource"/>, sources of the same type excepted.
    /// </summary>
    public static PreviousProviders From(IConfigurationBuilder builder, IConfigurationSource decryptorSource)
    {
        var sources = builder.Sources.ToList();
        var index = sources.IndexOf(decryptorSource);
        var builtProviders = builder is IConfigurationRoot root ? root.Providers.ToList() : null;

        // A ConfigurationManager exposes the providers of the earlier sources only when the decryptor source is being added.
        if (builtProviders is not null && builtProviders.Count != index)
        {
            builtProviders = null;
        }

        var providers = new List<(IConfigurationProvider, bool)>();
        for (var i = 0; i < index; i++)
        {
            var source = sources[i];
            var capturing = source as ProviderCapturingSource;
            if ((capturing?.Inner ?? source).GetType() == decryptorSource.GetType())
            {
                continue;
            }

            if (builtProviders is not null)
            {
                providers.Add((builtProviders[i], false));
            }
            else if (capturing?.Provider is not null)
            {
                providers.Add((capturing.Provider, false));
            }
            else
            {
                providers.Add((source.Build(builder), true));
            }
        }

        return new PreviousProviders(providers);
    }

    /// <summary>
    /// Build the providers of <paramref name="sources"/> in a new <see cref="ConfigurationBuilder"/>; they are loaded by <see cref="ToConfiguration"/>.
    /// </summary>
    public static PreviousProviders Build(IEnumerable<IConfigurationSource> sources)
    {
        var builder = new ConfigurationBuilder();
        return new PreviousProviders(sources.Select(source => (source.Build(builder), true)).ToList());
    }

    /// <summary>
    /// Wrap the sources of a <see cref="ConfigurationBuilder"/> so the decryptor can read the providers built from them.
    /// Nothing is done for a <see cref="ConfigurationManager"/>: its providers are already available.
    /// </summary>
    public static void Capture(IConfigurationBuilder builder)
    {
        if (builder is IConfigurationRoot)
        {
            return;
        }

        for (var i = 0; i < builder.Sources.Count; i++)
        {
            if (builder.Sources[i] is not ProviderCapturingSource)
            {
                builder.Sources[i] = new ProviderCapturingSource(builder.Sources[i]);
            }
        }
    }

    /// <summary>
    /// A read-only snapshot of the values of the providers, later providers winning as in a <see cref="IConfigurationRoot"/>.
    /// The providers already loaded by the configuration root are read, not loaded again.
    /// </summary>
    public IConfiguration ToConfiguration()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (provider, mustLoad) in _providers)
        {
            if (mustLoad)
            {
                provider.Load();
            }
            Collect(provider, null, data);
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static void Collect(IConfigurationProvider provider, string? parentPath, Dictionary<string, string?> data)
    {
        foreach (var key in provider.GetChildKeys([], parentPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = parentPath is null ? key : ConfigurationPath.Combine(parentPath, key);
            if (provider.TryGet(path, out var value))
            {
                data[path] = value;
            }
            Collect(provider, path, data);
        }
    }
}

/// <summary>
/// Build the inner source and keep the provider so the decryptor added after it can read its values.
/// </summary>
internal sealed class ProviderCapturingSource : IConfigurationSource
{
    public ProviderCapturingSource(IConfigurationSource inner)
    {
        Inner = inner;
    }

    public IConfigurationSource Inner { get; }

    public IConfigurationProvider? Provider { get; private set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        Provider = Inner.Build(builder);
        return Provider;
    }
}
