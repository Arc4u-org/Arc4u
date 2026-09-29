namespace Arc4u.OAuth2.Options;

/// <summary>
/// Inform if extra claims must be loaded during the authentication process.
/// </summary>
public class ClaimsFillerOptions
{
    // By default, a specific claims filler is needed to manage the right.
    /// <summary>Gets or sets a value indicating whether extra claims are loaded from the registered claims filler providers. The default is <see langword="true"/>.</summary>
    public bool LoadClaimsFromClaimsFillerProvider { get; set; } = true;

    // /// <summary>
    // /// The settings key to load the claims from. => registered as a named <see cref="SimpleKeyValueSettings>"/>
    // /// By default no on behalf of scenario is defined => OAuth2 must be added to perform the on behalf of scenario.
    // /// </summary>
    // public List<string> SettingsKeys { get; set; } = [];

    /// <summary>
    /// Claim keys that will not be part of the principal.
    /// </summary>
    public List<string> ClaimsToExclude { get; set; } = [];

    /// <summary>
    /// Define the expire claim key from the authority.
    /// </summary>
    public string ExpireClaim { get; set; } = "exp";
}
