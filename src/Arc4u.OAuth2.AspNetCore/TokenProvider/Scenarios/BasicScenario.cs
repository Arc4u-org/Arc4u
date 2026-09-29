using Arc4u.Configuration;
using Arc4u.OAuth2.Options;

namespace Arc4u.OAuth2.TokenProvider.Scenarios;

/// <summary>
/// <c>grant_type=password</c> style scenario taking the credential as a single Basic
/// <c>user:password</c> pair. The pair is split into its <c>User</c>/<c>Password</c> parts at
/// configuration time and projected into the keys consumed by the cache-backed
/// <see cref="CredentialTokenCacheTokenProvider"/>, which performs the STS call and caches the
/// resulting token.
/// <para>Reads its values from the <see cref="ClientTokenSettingsOptions.Settings"/> bag.</para>
/// <para>
/// Use the <see cref="UserPasswordScenario"/> instead when the user name and password are supplied
/// as two separate fields.
/// </para>
/// </summary>
public sealed class BasicScenario : IClientTokenScenario
{
    /// <summary>The discriminator under which this scenario is registered.</summary>
    public const string Name = "Basic";

    private const string ClientId = "ClientId";
    private const string Credential = "Credential";
    private const string ClientSecret = "ClientSecret";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> KnownKeys { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ClientId, Credential, ClientSecret };

    /// <inheritdoc/>
    public void Validate(string optionKey, ClientTokenSettingsOptions options)
    {
        new ClientTokenValidation(optionKey)
            .Require(options.Settings, ClientId)
            .Require(options.Settings, Credential)
            .ThrowIfInvalid();

        // The credential must be a 'user:password' pair so it can be split into its parts.
        if (options.Settings.TryGetValue(Credential, out var credential)
            && !string.IsNullOrWhiteSpace(credential)
            && credential.IndexOf(':') <= 0)
        {
            throw new ConfigurationException($"[{optionKey}] '{Credential}' must be a 'user:password' pair.");
        }
    }

    /// <inheritdoc/>
    public void WriteTo(string optionKey, ClientTokenSettingsOptions options, SimpleKeyValueSettings settings)
    {
        var s = options.Settings;

        var (user, password) = Split(s[Credential]);

        ClientTokenScenarioHelper.WriteUserPassword(
            options, settings, optionKey,
            clientId: s[ClientId],
            user: user,
            password: password,
            clientSecret: Get(s, ClientSecret));
    }

    private static (string User, string Password) Split(string credential)
    {
        var ix = credential.IndexOf(':');
        return (credential[..ix], credential[(ix + 1)..]);
    }

    private static string? Get(IReadOnlyDictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) ? value : null;
}
