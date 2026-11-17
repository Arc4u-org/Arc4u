using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Arc4u.Blazor;

/// <summary>Registers the Arc4u authentication state of Blazor circuits, with the periodic check that their sign-in still exists.</summary>
public static class CircuitRevalidationExtensions
{
    /// <summary>
    /// Turns on the revalidation of Blazor circuits: wraps the session store of the cookie in a <see cref="SessionKeyTicketStore"/> so each
    /// principal carries its session key, makes <see cref="AppPrincipalServerAuthenticationStateProvider"/> the
    /// <see cref="AuthenticationStateProvider"/> (replacing any other registration), registers <see cref="SessionStoreSignInValidator"/> as the
    /// <see cref="ISignInValidator"/> unless another one is registered, adds the <see cref="ApplicationContextCircuitHandler"/>, and validates
    /// the <see cref="CircuitRevalidationOptions"/> at startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the options; the defaults are used when it is <see langword="null"/>.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="OptionsValidationException">At startup: <see cref="CircuitRevalidationOptions.Interval"/> is not more than zero and at most <see cref="CircuitRevalidationOptions.MaxInterval"/>, or the cookie scheme is empty.</exception>
    /// <remarks>
    /// Without this call the principals carry no session key and the default validator cannot check anything: the circuits keep their user
    /// until they reconnect. It can be called before or after <c>AddOidcAuthentication</c>; call it after any other registration of
    /// <see cref="AuthenticationStateProvider"/>, which it removes.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddOidcAuthentication(builder.Configuration);
    /// builder.Services.AddAppPrincipalCircuitRevalidation(o => o.Interval = TimeSpan.FromMinutes(1));
    /// </code>
    /// </example>
    public static IServiceCollection AddAppPrincipalCircuitRevalidation(this IServiceCollection services, Action<CircuitRevalidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<CircuitRevalidationOptions>();
        if (configure is not null)
        {
            builder.Configure(configure);
        }

        return AddCore(services, builder);
    }

    /// <summary>
    /// Same as <see cref="AddAppPrincipalCircuitRevalidation(IServiceCollection, Action{CircuitRevalidationOptions}?)"/>, with the options read
    /// from a configuration section. The defaults are used when the section does not exist.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the options. The default is <see cref="CircuitRevalidationOptions.DefaultSectionName"/>.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddAppPrincipalCircuitRevalidation(this IServiceCollection services, IConfiguration configuration, string sectionName = CircuitRevalidationOptions.DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var builder = services.AddOptions<CircuitRevalidationOptions>().Bind(configuration.GetSection(sectionName));

        return AddCore(services, builder);
    }

    private static IServiceCollection AddCore(IServiceCollection services, OptionsBuilder<CircuitRevalidationOptions> builder)
    {
        builder
            .Validate(o => !o.Enabled || (o.Interval > TimeSpan.Zero && o.Interval <= CircuitRevalidationOptions.MaxInterval),
                $"{nameof(CircuitRevalidationOptions)}.{nameof(CircuitRevalidationOptions.Interval)} must be more than zero and at most {CircuitRevalidationOptions.MaxInterval}.")
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.CookieScheme),
                $"{nameof(CircuitRevalidationOptions)}.{nameof(CircuitRevalidationOptions.CookieScheme)} must be set.")
            .ValidateOnStart();

        services.TryAddSingleton<ISignInValidator, SessionStoreSignInValidator>();

        // The principals read from the cookie's session store carry their session key, which the validator checks. A closed options factory
        // takes precedence over the open generic one whatever the order, and runs after every post-configure step (see SessionKeyCookieOptionsFactory).
        services.Replace(ServiceDescriptor.Transient<IOptionsFactory<CookieAuthenticationOptions>, SessionKeyCookieOptionsFactory>());

        services.RemoveAll<AuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider, AppPrincipalServerAuthenticationStateProvider>();

        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, ApplicationContextCircuitHandler>());

        return services;
    }
}
