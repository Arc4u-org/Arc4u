using Arc4u.Configuration;
using Arc4u.OAuth2.Options;

namespace Arc4u.OAuth2.TokenProvider.Scenarios;

/// <summary>
/// <c>grant_type=password</c> style scenario taking the user name and password as two separate
/// fields. The values are projected into the <c>User</c>/<c>Password</c> keys consumed by the
/// cache-backed <see cref="CredentialTokenCacheTokenProvider"/>, which performs the STS call and
/// caches the resulting token.
/// <para>Reads its values from the <see cref="ClientTokenSettingsOptions.Settings"/> bag.</para>
/// <para>
/// Use the <see cref="BasicScenario"/> instead when the credential is supplied as a single
/// <c>user:password</c> (Basic) pair.
/// </para>
/// </summary>
public sealed class UserPasswordScenario : IClientTokenScenario
{
    /// <summary>The discriminator under which this scenario is registered.</summary>
    public const string Name = "UserPassword";

    private const string ClientId = "ClientId";
    private const string User = "User";
    private const string Password = "Password";
    private const string ClientSecret = "ClientSecret";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> KnownKeys { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ClientId, User, Password, ClientSecret };

    /// <inheritdoc/>
    public void Validate(string optionKey, ClientTokenSettingsOptions options)
    {
        new ClientTokenValidation(optionKey)
            .Require(options.Settings, ClientId)
            .Require(options.Settings, User)
            .Require(options.Settings, Password)
            .ThrowIfInvalid();
    }

    /// <inheritdoc/>
    public void WriteTo(string optionKey, ClientTokenSettingsOptions options, SimpleKeyValueSettings settings)
    {
        var s = options.Settings;

        ClientTokenScenarioHelper.WriteUserPassword(
            options, settings, optionKey,
            clientId: s[ClientId],
            user: s[User],
            password: s[Password],
            clientSecret: Get(s, ClientSecret));
    }

    private static string? Get(IReadOnlyDictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) ? value : null;
}
