using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;

/// <summary>
/// Registers the unified <c>Authentication:ClientTokens</c> section: a single section where
/// heterogeneous client token scenarios coexist, each entry discriminated by its
/// <see cref="ClientTokenSettingsOptions.Scenario"/> property and projected into a named
/// <see cref="SimpleKeyValueSettings"/>.
/// </summary>
public static class ClientTokensExtension
{
    /// <summary>
    /// Resolves an <see cref="IClientTokenScenario"/> from its discriminator (the
    /// <see cref="ClientTokenSettingsOptions.Scenario"/> value), or <c>null</c> when none matches.
    /// <para>
    /// Defaults to <see cref="DefaultScenario"/> (the built-in scenarios). A framework user can
    /// reconfigure it via <see cref="SetScenarioResolver"/> — typically by handling their own
    /// discriminators and delegating to <see cref="DefaultScenario"/> for the rest.
    /// </para>
    /// </summary>
    public static Func<string, IClientTokenScenario?> Scenario => discriminator => _scenario(discriminator);

    private static Func<string, IClientTokenScenario?> _scenario = DefaultScenario;

    /// <summary>
    /// Reconfigures the scenario resolver. Delegate to <see cref="DefaultScenario"/> to keep the built-ins.
    /// </summary>
    /// <example>
    /// <code>
    /// ClientTokensExtension.SetScenarioResolver(discriminator => discriminator switch
    /// {
    ///     "MyCustom" => new MyCustomScenario(),
    ///     _ => ClientTokensExtension.DefaultScenario(discriminator)
    /// });
    /// </code>
    /// </example>
    public static void SetScenarioResolver([DisallowNull] Func<string, IClientTokenScenario?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _scenario = resolver;
    }

    /// <summary>
    /// The default resolver: maps the built-in discriminators
    /// (<see cref="UserPasswordScenario.Name"/>, <see cref="BasicScenario.Name"/>,
    /// <see cref="ClientCredentialsScenario.Name"/>) to their scenarios.
    /// </summary>
    public static IClientTokenScenario? DefaultScenario(string discriminator) => discriminator switch
    {
        UserPasswordScenario.Name => new UserPasswordScenario(),
        BasicScenario.Name => new BasicScenario(),
        ClientCredentialsScenario.Name => new ClientCredentialsScenario(),
        _ => null
    };

    /// <summary>
    /// Reads the <paramref name="sectionName"/> section and registers a named
    /// <see cref="SimpleKeyValueSettings"/> per entry, selecting the matching
    /// <see cref="IClientTokenScenario"/> by the entry's discriminator through <see cref="Scenario"/>.
    /// Nothing is registered when the section does not exist or is empty. An entry is registered only once per service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read the section from, for example <c>builder.Configuration</c>.</param>
    /// <param name="sectionName">The section to read. The default is <c>Authentication:ClientTokens</c>.</param>
    /// <exception cref="ConfigurationException">An entry has no <c>Scenario</c>, an unknown scenario, an empty authentication type, or is invalid for its scenario.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddClientTokens(builder.Configuration);
    /// </code>
    /// </example>
    public static void AddClientTokens(this IServiceCollection services,
        [DisallowNull] IConfiguration configuration,
        [DisallowNull] string sectionName = "Authentication:ClientTokens")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        var section = configuration.GetSection(sectionName);

        if (section is null || !section.Exists())
        {
            return;
        }

        var entries = section.Get<Dictionary<string, ClientTokenSettingsOptions>>();

        if (entries is null || entries.Count == 0)
        {
            return;
        }

        foreach (var (optionKey, options) in entries)
        {
            Register(services, optionKey, options);
        }
    }

    private static void Register(IServiceCollection services, string optionKey, ClientTokenSettingsOptions options)
    {
        // AddClientTokens may be reached from several composition paths (e.g. a service registration
        // and an HttpClient handler pipeline). Registering the named-options projection twice for the
        // same entry would make a single IOptions Create run WriteTo twice on the same instance,
        // throwing a duplicate-key exception. Configure each entry only once per service collection.
        if (!RegisteredKeys(services).Add(optionKey))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Scenario))
        {
            throw new ConfigurationException($"[{optionKey}] The 'Scenario' discriminator must be filled.");
        }

        var scenario = Scenario(options.Scenario)
            ?? throw new ConfigurationException($"[{optionKey}] No client token scenario is registered for the discriminator '{options.Scenario}'.");

        if (string.IsNullOrWhiteSpace(options.AuthenticationType))
        {
            throw new ConfigurationException($"[{optionKey}] AuthenticationType must be filled.");
        }

        // Eager validation: fail fast, before the service provider is built.
        scenario.Validate(optionKey, options);

        if (options.Authority is not null)
        {
            services.AddAuthority(authOptions =>
            {
                authOptions.SetData(options.Authority.Url, options.Authority.TokenEndpoint, options.Authority.Issuer, options.Authority.MetaDataAddress);
            }, optionKey);
        }

        // Projection is deferred into the named options callback; it only writes already-validated data.
        services.Configure<SimpleKeyValueSettings>(optionKey, settings =>
        {
            scenario.WriteTo(optionKey, options, settings);

            // Any Settings key the scenario does not claim is forwarded verbatim as an extra request parameter.
            var extra = options.Settings.Where(kv => !scenario.KnownKeys.Contains(kv.Key)).ToList();
            if (extra.Count > 0)
            {
                settings.Add(TokenKeys.ExtraParameters, ExtraParametersEncoder.Encode(extra));
            }
        });
    }

    /// <summary>
    /// Returns the set of entry keys already configured on this service collection, so the same
    /// entry is never registered twice across multiple <see cref="AddClientTokens"/> invocations.
    /// </summary>
    private static HashSet<string> RegisteredKeys(IServiceCollection services)
    {
        var marker = (RegisteredClientTokens?)services
            .FirstOrDefault(d => d.ServiceType == typeof(RegisteredClientTokens))?.ImplementationInstance;

        if (marker is null)
        {
            marker = new RegisteredClientTokens();
            services.AddSingleton(marker);
        }

        return marker.Keys;
    }

    private sealed class RegisteredClientTokens
    {
        public HashSet<string> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
