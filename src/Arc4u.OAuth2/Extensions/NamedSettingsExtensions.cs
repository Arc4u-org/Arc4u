using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Extensions;

/// <summary>Resolves settings registered by name.</summary>
public static class NamedSettingsExtensions
{
    /// <summary>
    /// Tries to resolve the named <see cref="SimpleKeyValueSettings"/> options registered with <paramref name="name"/>
    /// (<c>services.Configure&lt;SimpleKeyValueSettings&gt;(name, ...)</c>, as all the Arc4u extension methods do).
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="name">The name of the settings.</param>
    /// <param name="settings">The settings, or <see langword="null"/> when none exist with that name.</param>
    /// <returns><see langword="true"/> when settings were found; otherwise <see langword="false"/>.</returns>
    public static bool TryGetNamedSettings(this IServiceProvider serviceProvider, string name, [NotNullWhen(true)] out IKeyValueSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(name);

        // Options.Get never returns null: settings that were not configured are empty.
        var options = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()?.Get(name);

        settings = options is not null && options.Values.Count > 0 ? options : null;

        return settings is not null;
    }
}
