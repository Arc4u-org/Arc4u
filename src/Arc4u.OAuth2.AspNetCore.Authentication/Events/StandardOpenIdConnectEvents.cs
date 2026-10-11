using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Events
{
    /// <summary>
    /// The default <see cref="OpenIdConnectEvents"/>. It validates the audience and the issuer of the access token received from the token endpoint
    /// (the issuer must be <see cref="AuthorityOptions.Issuer"/> of the default authority when it is set, or the issuer of its metadata otherwise), forces https redirect urls (except for localhost),
    /// adds the extra authorization and token request parameters (see <see cref="ApiExtraContextAuthenticationOption"/>), and answers failures with a simple html message.
    /// </summary>
    public sealed partial class StandardOpenIdConnectEvents : OpenIdConnectEvents

    {
        private readonly ILogger<StandardOpenIdConnectEvents> _logger;
        private readonly OidcAuthenticationOptions _hybridOptions;
        private readonly IOptionsMonitor<SimpleKeyValueSettings> _openIdOptions;
        private readonly IOptionsMonitor<ApiExtraContextAuthenticationOption> _extraContextOptions;

        /// <summary>Initializes a new instance of the <see cref="StandardOpenIdConnectEvents"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="oidcOptions">The OpenID Connect authentication options.</param>
        /// <param name="openIdOptions">The named OpenID settings.</param>
        /// <param name="extraContextOptions">The extra parameters added to the authorization and token requests.</param>
        public StandardOpenIdConnectEvents(ILogger<StandardOpenIdConnectEvents> logger, IOptionsMonitor<OidcAuthenticationOptions> oidcOptions, IOptionsMonitor<SimpleKeyValueSettings> openIdOptions, IOptionsMonitor<ApiExtraContextAuthenticationOption> extraContextOptions)
        {
            _hybridOptions = oidcOptions.CurrentValue;
            _openIdOptions = openIdOptions;
            _logger = logger;
            _extraContextOptions = extraContextOptions;
        }

        [GeneratedRegex(@"\b(?:http:\/\/localhost|https:\/\/)\b", RegexOptions.IgnoreCase)]
        private static partial Regex HttpRegex();

        /// <inheritdoc/>
        public override async Task TokenResponseReceived(TokenResponseReceivedContext context)
        {
            if (string.IsNullOrWhiteSpace(context.TokenEndpointResponse.AccessToken))
            {
                return;
            }

            var jwtToken = new JwtSecurityToken(context.TokenEndpointResponse.AccessToken);
            var options = _openIdOptions.Get(Constants.CookiesAuthenticationType);

            // The Audiences key is not registered when the OpenId settings disable the audience validation.
            if (_hybridOptions.ValidateAudience && options.Values.TryGetValue(TokenKeys.Audiences, out var audiences))
            {
                if (!jwtToken.Audiences.Any(aud => audiences.Contains(aud)))
                {
                    _logger.Technical()
                        .LogError("Audience(s) {audience} is not in the list of allowed audience(s): {audiences}.",
                            jwtToken.Audiences,
                            audiences);

                    context.Fail("Invalid audience");

                    return;
                }
            }

            if (!_hybridOptions.ValidateAuthority)
            {
                return;
            }

            // Same rule as the bearer tokens: the configured issuer, or the issuer of the metadata.
            var expectedIssuer = await GetExpectedIssuerAsync(context).ConfigureAwait(false);

            if (string.Equals(jwtToken.Issuer, expectedIssuer, StringComparison.Ordinal))
            {
                return;
            }

            _logger.Technical()
                .LogError("Authority {authority} is not in the expected one: {authority_from_config}.",
                    jwtToken.Issuer,
                    expectedIssuer);

            context.Fail("Invalid authority");
        }

        private async Task<string> GetExpectedIssuerAsync(TokenResponseReceivedContext context)
        {
            if (_hybridOptions.DefaultAuthority.Issuer is not null)
            {
                return _hybridOptions.DefaultAuthority.Issuer.ToString();
            }

            var configuration = context.Options.Configuration;
            if (configuration is null && context.Options.ConfigurationManager is not null)
            {
                // Cached by the OpenID Connect handler, which has already read it to start the sign-in.
                configuration = await context.Options.ConfigurationManager.GetConfigurationAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
            }

            return configuration?.Issuer ?? _hybridOptions.DefaultAuthority.Url.AbsoluteUri;
        }

        /// <inheritdoc/>
        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            // force https for redirect uri but for localhost.
            if (!HttpRegex().IsMatch(context.ProtocolMessage.RedirectUri))
            {
                context.ProtocolMessage.RedirectUri = context.ProtocolMessage.RedirectUri.Replace("http://", "https://");
            }

            // Has been introduced for AzureAD => works also for Keykloack.
            context.ProtocolMessage.State = Guid.NewGuid().ToString();

            // Some Idp require extra parameters to link application definition to the right Api context.
            foreach (var extra in _extraContextOptions.CurrentValue.AuthorizationParameters.Values)
            {
                context.ProtocolMessage.SetParameter(extra.Key, extra.Value);
            }

            return base.RedirectToIdentityProvider(context);
        }

        /// <inheritdoc/>
        public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            // Some Idp require extra parameters to link application definition to the right Api context.
            foreach (var extra in _extraContextOptions.CurrentValue.TokenParameters.Values)
            {
                context.TokenEndpointRequest?.Parameters.Add(extra.Key, extra.Value);
            }

            return base.AuthorizationCodeReceived(context);
        }

        /// <inheritdoc/>
        public override async Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.HandleResponse();

            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain";

            _logger.Technical().LogException(context.Exception);

            await context.Response.WriteAsync("<html><p>You are not authenticated.</p></html>").ConfigureAwait(false);

        }

        /// <inheritdoc/>
        public override Task AccessDenied(AccessDeniedContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.HandleResponse();

            return context.Response.WriteAsync("<html><p>You are not authorized to use this api</p></html>");

        }

        /// <inheritdoc/>
        public override Task RemoteFailure(RemoteFailureContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.HandleResponse();

            return context.Response.WriteAsync($"<html><p>There was an issue during the request: {context.Failure?.Message ?? "Unknown error"}.</p></html>");
        }
    }
}
