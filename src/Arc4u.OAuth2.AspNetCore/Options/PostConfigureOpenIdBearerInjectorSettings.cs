using Arc4u.Configuration;
using Microsoft.Extensions.Options;
using DefaultOptions = Microsoft.Extensions.Options.Options;

namespace Arc4u.OAuth2.Options;
/// <summary>
/// Fills the default (unnamed) <see cref="OpenIdBearerInjectorSettingsOptions"/> from the named <see cref="SimpleKeyValueSettings"/> designated by <see cref="OpenIdBearerInjectorOptions"/>.
/// </summary>
public class PostConfigureOpenIdBearerInjectorSettings : IPostConfigureOptions<OpenIdBearerInjectorSettingsOptions>
{
    /// <summary>Initializes a new instance of the <see cref="PostConfigureOpenIdBearerInjectorSettings"/> class.</summary>
    /// <param name="openIdOptions">The options giving the names of the settings.</param>
    /// <param name="simpleKeyOptions">The named key/value settings.</param>
    public PostConfigureOpenIdBearerInjectorSettings(IOptions<OpenIdBearerInjectorOptions> openIdOptions,
                                                     IOptionsMonitor<SimpleKeyValueSettings> simpleKeyOptions)
    {
        _opendIdOptions = openIdOptions.Value;
        _simpleKeyOptions = simpleKeyOptions;
    }

    readonly OpenIdBearerInjectorOptions _opendIdOptions;
    readonly IOptionsMonitor<SimpleKeyValueSettings> _simpleKeyOptions;

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIdBearerInjectorSettingsOptions options)
    {
        if (name == DefaultOptions.DefaultName)
        {
            options.OnBehalfOfOpenIdSettings = _simpleKeyOptions.Get(_opendIdOptions.OnBehalfOfOpenIdSettingsKey);
            options.OboProviderKey = _opendIdOptions.OboProviderKey;
            options.OpenIdSettings = _simpleKeyOptions.Get(_opendIdOptions.OpenIdSettingsKey);
        }
    }
}
