using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arc4u.OAuth2.Options;

[JsonSerializable(typeof(OpenIdConfiguration))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class OpenIdConfigurationJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Only define the properties that are being used when calling the well-known Oidc endpoint.
/// </summary>
sealed class OpenIdConfiguration
{
    public required Uri Token_endpoint { get; set; }

    public required Uri issuer { get; set; }

    public Uri? access_token_issuer { get; set; }

    public Uri? end_session_endpoint { get; set; }
}

/// <summary>
/// Describes an OpenID Connect authority (identity provider) and its endpoints.
/// Endpoints that are not set explicitly are discovered from the metadata document (<c>.well-known/openid-configuration</c>) of the authority.
/// </summary>
public class AuthorityOptions
{
    /// <summary>
    /// For Serialization.
    /// </summary>
    public AuthorityOptions()
    {
        Url = new Uri("about:blank");
    }

    /// <summary>Initializes a new instance of the <see cref="AuthorityOptions"/> class.</summary>
    /// <param name="url">The base url of the authority.</param>
    /// <param name="tokenEndpoint">The token endpoint, or <see langword="null"/> to discover it from the metadata.</param>
    /// <param name="issuer">The issuer, or <see langword="null"/> to discover it from the metadata.</param>
    /// <param name="metadataAddress">The metadata address, or <see langword="null"/> to use the standard <c>.well-known/openid-configuration</c> path below <paramref name="url"/>.</param>
    public AuthorityOptions(Uri url, Uri? tokenEndpoint, Uri? issuer, Uri? metadataAddress)
    {
        Url = url;
        TokenEndpoint = tokenEndpoint;
        MetaDataAddress = metadataAddress;
        Issuer = issuer;
    }

    /// <summary>Sets the base url and the optional endpoints of the authority.</summary>
    /// <param name="url">The base url of the authority.</param>
    /// <param name="tokenEndpoint">The token endpoint, or <see langword="null"/> to discover it from the metadata.</param>
    /// <param name="issuer">The issuer, or <see langword="null"/> to discover it from the metadata.</param>
    /// <param name="metadataAddress">The metadata address, or <see langword="null"/> to use the standard <c>.well-known/openid-configuration</c> path below <paramref name="url"/>.</param>
    public void SetData(Uri url, Uri? tokenEndpoint, Uri? issuer, Uri? metadataAddress)
    {
        Url = url;
        TokenEndpoint = tokenEndpoint;
        MetaDataAddress = metadataAddress;
        Issuer = issuer;
    }

    /// <summary>Gets or sets the base url of the authority.</summary>
    public Uri Url { get; set; }

    /// <summary>Gets or sets the token endpoint. When <see langword="null"/> it is read from the metadata by <see cref="GetEndpointAsync(CancellationToken)"/>.</summary>
    public Uri? TokenEndpoint { get; set; }

    /// <summary>Gets or sets the issuer of the access tokens. When <see langword="null"/> it is read from the metadata by <see cref="GetIssuerAsync(CancellationToken)"/>.</summary>
    public Uri? Issuer { get; set; }

    /// <summary>Gets or sets the address of the OpenID Connect metadata document. When <see langword="null"/>, <see cref="GetMetaDataAddress"/> builds it from <see cref="Url"/>.</summary>
    public Uri? MetaDataAddress { get; set; }

    /// <summary>Gets or sets the end session (sign-out) endpoint. When <see langword="null"/> it is read from the metadata by <see cref="GetEndSessionEndpointAsync(CancellationToken)"/>.</summary>
    public Uri? EndSessionEndpoint { get; set; }

    /// <summary>Gets or sets the maximum time during which a failing token request is retried. When <see langword="null"/> the token providers use 90 seconds.</summary>
    public TimeSpan? RetryInterval { get; set; }

    /// <summary>
    /// Gets the address of the OpenID Connect metadata document. When <see cref="MetaDataAddress"/> is not set, the standard
    /// discovery path (<c>/.well-known/openid-configuration</c>) is appended to <see cref="Url"/> and stored in <see cref="MetaDataAddress"/>.
    /// If the authority uses another path, provide the full metadata address.
    /// </summary>
    /// <returns>The address of the metadata document.</returns>
    public Uri GetMetaDataAddress()
    {
        if (MetaDataAddress == null)
        {
            var uriBuilder = new UriBuilder(Url);
            // See section 4 of https://openid.net/specs/openid-connect-discovery-1_0.html
            uriBuilder.Path += "/.well-known/openid-configuration";
            uriBuilder.Path = uriBuilder.Path.Replace("//", "/");
            MetaDataAddress = uriBuilder.Uri;
        }
        return MetaDataAddress;
    }

    /// <summary>Gets a value indicating whether the host of <see cref="Url"/> is <c>localhost</c>, <c>127.0.0.1</c> or <c>::1</c>.</summary>
    public bool IsLocalHost => Url.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                               Url.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                               Url.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the metadata address relative to <see cref="Url"/>, for authorities whose metadata path does not follow the standard.</summary>
    /// <returns>The <see cref="MetaDataAddress"/> with the <see cref="Url"/> part removed.</returns>
    public string GetRelativeMetaDataAddress()
    {
        // some metadata addresses do not follow the standard.
        // Remove from the MetaDataAddress the Url path!
        if (MetaDataAddress == null)
        {
            GetMetaDataAddress();
        }
        return MetaDataAddress!.ToString().Replace(Url.ToString(), string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Gets the token endpoint, downloading the metadata document when it is not yet known. The metadata also fills <see cref="EndSessionEndpoint"/> and <see cref="Issuer"/>.</summary>
    /// <param name="cancellationToken">A token to cancel the metadata request.</param>
    /// <returns>The token endpoint.</returns>
    public async Task<Uri> GetEndpointAsync(CancellationToken cancellationToken)
    {
        if (TokenEndpoint is null)
        {
            using var client = new HttpClient();

            var stream = await client.GetStreamAsync(GetMetaDataAddress(), cancellationToken).ConfigureAwait(false);
            var openIdConfiguration = JsonSerializer.Deserialize(stream, OpenIdConfigurationJsonContext.Default.OpenIdConfiguration);

            EndSessionEndpoint = openIdConfiguration!.end_session_endpoint;
            TokenEndpoint = openIdConfiguration!.Token_endpoint;
            Issuer = openIdConfiguration.access_token_issuer ?? openIdConfiguration.issuer;
        }
        return TokenEndpoint;
    }

    /// <summary>Gets the end session endpoint, downloading the metadata document when it is not yet known. The metadata also fills <see cref="TokenEndpoint"/> and <see cref="Issuer"/>.</summary>
    /// <param name="cancellationToken">A token to cancel the metadata request.</param>
    /// <returns>The end session endpoint.</returns>
    public async Task<Uri> GetEndSessionEndpointAsync(CancellationToken cancellationToken)
    {
        if (EndSessionEndpoint is null)
        {
            using var client = new HttpClient();

            var stream = await client.GetStreamAsync(GetMetaDataAddress(), cancellationToken).ConfigureAwait(false);
            var openIdConfiguration = JsonSerializer.Deserialize(stream, OpenIdConfigurationJsonContext.Default.OpenIdConfiguration);

            EndSessionEndpoint = openIdConfiguration!.end_session_endpoint;
            TokenEndpoint = openIdConfiguration!.Token_endpoint;
            Issuer = openIdConfiguration.access_token_issuer ?? openIdConfiguration.issuer;
        }
        return EndSessionEndpoint;
    }
    /// <summary>Gets the issuer, downloading the metadata document when it is not yet known. The metadata also fills <see cref="TokenEndpoint"/> and <see cref="EndSessionEndpoint"/>.</summary>
    /// <param name="cancellationToken">A token to cancel the metadata request.</param>
    /// <returns>The issuer, which is the <c>access_token_issuer</c> of the metadata when present, otherwise its <c>issuer</c>.</returns>
    public async Task<Uri> GetIssuerAsync(CancellationToken cancellationToken)
    {
        if (Issuer is null)
        {
            using var client = new HttpClient();

            var stream = await client.GetStreamAsync(GetMetaDataAddress(), cancellationToken).ConfigureAwait(false);
            var openIdConfiguration = JsonSerializer.Deserialize(stream, OpenIdConfigurationJsonContext.Default.OpenIdConfiguration);

            EndSessionEndpoint = openIdConfiguration!.end_session_endpoint;
            TokenEndpoint = openIdConfiguration!.Token_endpoint;
            Issuer = openIdConfiguration.access_token_issuer ?? openIdConfiguration.issuer;
        }
        return Issuer;
    }
}
