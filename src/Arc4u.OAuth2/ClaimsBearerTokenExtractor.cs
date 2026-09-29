using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Serialization.Json;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Security.Principal;

[JsonSerializable(typeof(IEnumerable<ClaimDto>))]
internal partial class ClaimsBearerTokenContext : JsonSerializerContext
{
}

/// <summary>
/// An <see cref="IClaimsFiller"/> that extracts the claims of a JWT access token.
/// The token is taken from the <see cref="ClaimsIdentity.BootstrapContext"/> of the identity or, when there is none, requested
/// from the <see cref="ITokenProvider"/> registered for the identity authentication type.
/// </summary>
[Export(typeof(IClaimsFiller))]
public class ClaimsBearerTokenExtractor : IClaimsFiller
{
    /// <summary>Initializes a new instance of the <see cref="ClaimsBearerTokenExtractor"/> class.</summary>
    /// <param name="settings">The named token provider settings, keyed by authentication type.</param>
    /// <param name="serviceProvider">The service provider used to resolve the keyed <see cref="ITokenProvider"/>.</param>
    /// <param name="logger">The logger.</param>
    public ClaimsBearerTokenExtractor(IOptionsMonitor<SimpleKeyValueSettings> settings, IServiceProvider serviceProvider, ILogger<ClaimsBearerTokenExtractor> logger)
    {
        _settings = settings;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private readonly IOptionsMonitor<SimpleKeyValueSettings> _settings;
    private readonly ILogger<ClaimsBearerTokenExtractor> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Reads the claims contained in the access token of the identity.
    /// </summary>
    /// <param name="identity">The identity for which the claims are loaded. It must be a <see cref="ClaimsIdentity"/>.</param>
    /// <returns>The claims found in the token. The list is empty when the identity is not a <see cref="ClaimsIdentity"/>, no token provider is configured for it, or the token cannot be obtained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
    public async Task<IEnumerable<ClaimDto>> GetAsync(IIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var result = new List<ClaimDto>();

        if (identity is not ClaimsIdentity claimsIdentity)
        {
            _logger.Technical().LogError($"The identity received is not of type ClaimsIdentity.");
            return result;
        }

        if (null == claimsIdentity.BootstrapContext && _settings.Get(identity.AuthenticationType).Values.Count == 0)
        {
            _logger.Technical().LogSkipFetchingClaims(identity.AuthenticationType ?? "No AuthenticationType.");
            return result;
        }

        try
        {
            JwtSecurityToken? bearerToken = null;
            if (null != claimsIdentity.BootstrapContext)
            {
                bearerToken = new JwtSecurityToken(claimsIdentity.BootstrapContext.ToString());
            }
            else
            {
                // find the Provider for the AuthenticationType!
                var providerSettings = _settings.Get(identity.AuthenticationType).Values;

                var provider = _serviceProvider.GetKeyedService<ITokenProvider>(providerSettings[TokenKeys.ProviderIdKey]);

                if (null == provider)
                {
                    throw new InvalidOperationException($"No token provider named: {providerSettings[TokenKeys.ProviderIdKey]} is registered.");
                }

                _logger.Technical().LogRequestingAuthenticationToken();
                var tokenInfoResult = await provider.GetTokenAsync(new SimpleKeyValueSettings(providerSettings), claimsIdentity).ConfigureAwait(false);

                if (tokenInfoResult.IsFailed)
                {
                    _logger.Technical().LogNoToken();
                    tokenInfoResult.Log();
                    return result;
                }

                bearerToken = new JwtSecurityToken(tokenInfoResult.Value.Token);
            }

            result.AddRange(bearerToken.Claims.Select(c => new ClaimDto(c.Type, c.Value)));

        }
        catch (Exception exception)
        {
            _logger.Technical().LogException(exception);
        }

        return result;
    }
}

