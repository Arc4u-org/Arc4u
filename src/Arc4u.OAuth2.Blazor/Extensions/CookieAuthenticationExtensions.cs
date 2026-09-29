using Arc4u.Blazor.Handlers;
using Arc4u.Configuration;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Blazor.Options;

/// <summary>
/// Provides extension methods to register and configure <see cref="AuthenticationCookieSettingsOption"/> for handling authentication cookies.
/// Options can be configured directly from application configuration or via custom code using an action delegate.
/// </summary>
public static class CookieAuthenticationExtensions
{
    /// <summary>
    /// Registers the cookie authentication of a Blazor WebAssembly application from code.
    /// The settings are registered as the key/value settings <paramref name="sectionKey"/> and an <see cref="HttpClient"/> named <see cref="AuthenticationCookieSettingsOption.HttpClientName"/> is added, whose requests include the browser cookies (see <see cref="AttachCookiesHandler"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="option">The action that configures the <see cref="AuthenticationCookieSettingsOption"/>.</param>
    /// <param name="sectionKey">The name of the key/value settings. The default is <c>OAuth2</c>.</param>
    /// <exception cref="MissingFieldException">A mandatory value of the options is empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddAuthenticationCookie(options =>
    /// {
    ///     options.BaseUri = new Uri(builder.HostEnvironment.BaseAddress);
    ///     options.TokenRequestUrl = "/authentication/token";
    /// });
    /// </code>
    /// </example>
    public static void AddAuthenticationCookie(this IServiceCollection services,
                                                    Action<AuthenticationCookieSettingsOption> option,
                                                    string sectionKey = "OAuth2")
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(sectionKey);

        var authenticationCookieSettingsOption = services.ReadAuthenticationCookieSettingsOption(option);

        ConfigureAuthenticationCookieSettings(services, sectionKey, authenticationCookieSettingsOption);
    }

    /// <summary>Registers the cookie authentication of a Blazor WebAssembly application from a configuration section. See the other overload for the registered services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the <see cref="AuthenticationCookieSettingsOption"/>. The default is <c>Authentication:OAuth2.Settings</c>.</param>
    /// <param name="sectionKey">The name of the key/value settings. The default is <c>OAuth2</c>.</param>
    /// <exception cref="ArgumentException">The section does not exist.</exception>
    /// <exception cref="MissingFieldException">A mandatory value of the options is empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddAuthenticationCookie(builder.Configuration);
    /// </code>
    /// </example>
    public static void AddAuthenticationCookie(this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Authentication:OAuth2.Settings", string sectionKey = "OAuth2")
    {
        var authenticationCookieSettingsOption =
            services.ReadAuthenticationCookieSettingsOption(configuration, sectionName);

        ConfigureAuthenticationCookieSettings(services, sectionKey, authenticationCookieSettingsOption);
    }

    private static void ConfigureAuthenticationCookieSettings(IServiceCollection services,
                                                              string sectionKey,
                                                              AuthenticationCookieSettingsOption authenticationCookieSettingsOption)
    {
        // Register the settings for the cookie client token provider.
        void SettingsFiller(SimpleKeyValueSettings keyOptions)
        {
            keyOptions.Add(TokenKeys.ProviderIdKey, authenticationCookieSettingsOption.ProviderId);
            keyOptions.Add(TokenKeys.TokenRequestUrl, authenticationCookieSettingsOption.TokenRequestUrl);
            keyOptions.Add(TokenKeys.HttpClientName, authenticationCookieSettingsOption.HttpClientName);
        }

        services.Configure<SimpleKeyValueSettings>(sectionKey, SettingsFiller);

        services.AddHttpClient(authenticationCookieSettingsOption.HttpClientName,
                client => { client.BaseAddress = authenticationCookieSettingsOption.BaseUri; })
                .AddHttpMessageHandler<AttachCookiesHandler>();
    }

    /// <summary>Builds and validates the <see cref="AuthenticationCookieSettingsOption"/> from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="option">The action that configures the options.</param>
    /// <returns>The validated options.</returns>
    /// <exception cref="MissingFieldException">The client name, provider id, token request url or base uri is empty.</exception>
    public static AuthenticationCookieSettingsOption ReadAuthenticationCookieSettingsOption(this IServiceCollection services, Action<AuthenticationCookieSettingsOption> option)
    {
        var validate = new AuthenticationCookieSettingsOption();
        option(validate);

        if (string.IsNullOrWhiteSpace(validate.HttpClientName))
        {
            throw new MissingFieldException("HttpClientName field is missing.");
        }

        if (string.IsNullOrWhiteSpace(validate.ProviderId))
        {
            throw new MissingFieldException("ProviderId field is missing.");
        }

        if (string.IsNullOrWhiteSpace(validate.TokenRequestUrl))
        {
            throw new MissingFieldException("TokenRequestUrl field is missing.");
        }

        if (validate.BaseUri is null || string.IsNullOrWhiteSpace(validate.BaseUri.AbsoluteUri))
        {
            throw new MissingFieldException("BaseUri field is missing.");
        }

        return validate;
    }

    /// <summary>Builds and validates the <see cref="AuthenticationCookieSettingsOption"/> from a configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the options. The default of this method is <c>Authentication:OAuth.Settings</c>, which differs from the <c>Authentication:OAuth2.Settings</c> default of <c>AddAuthenticationCookie</c>.</param>
    /// <returns>The validated options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sectionName"/> is empty or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The section does not exist.</exception>
    /// <exception cref="MissingFieldException">The client name, provider id, token request url or base uri is empty.</exception>
    public static AuthenticationCookieSettingsOption ReadAuthenticationCookieSettingsOption(this IServiceCollection services,
                                                                                                 IConfiguration configuration,
                                                                                                 string sectionName = "Authentication:OAuth.Settings")
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName));
        }

        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);

        if (!section.Exists())
        {
            throw new ArgumentException($"Section {sectionName} not found.");
        }

        var cookieOption = configuration.GetSection(sectionName).Get<AuthenticationCookieSettingsOption>();

        if (cookieOption is null)
        {
            throw new NullReferenceException(nameof(cookieOption));
        }

        return ReadAuthenticationCookieSettingsOption(services, option =>
        {
            option.BaseUri = cookieOption.BaseUri;
            option.HttpClientName = cookieOption.HttpClientName;
            option.ProviderId = cookieOption.ProviderId;
            option.TokenRequestUrl = cookieOption.TokenRequestUrl;
        });
    }
}

