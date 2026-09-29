namespace Arc4u.OAuth2.Options;

/// <summary>
/// Gives the configuration section paths from which the extra authorization and token request parameters are read.
/// </summary>
public class ApiExtraContextAuthenticationSectionOption
{
    /// <summary>
    /// Define the path
    /// </summary>
    public string AuthorizationEndpointSectionPath { get; set; } = "Authentication:OpenId.Settings:AuthorizationEndpoint";

    /// <summary>
    /// Gets or sets the configuration section holding the extra parameters sent to the token endpoint.
    /// The default is <c>Authentication:OpenId.Settings:TokenEndpoint</c>.
    /// </summary>
    public string TokenEndpointSectionPath { get; set; } = "Authentication:OpenId.Settings:TokenEndpoint";
}
