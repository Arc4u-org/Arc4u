using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Microsoft.Extensions.Options;

namespace Arc4u.Security.Principal;

/// <summary>
/// An <see cref="IClaimProfileFiller"/> that builds the <see cref="UserProfile"/> from the claims of a <see cref="System.Security.Claims.ClaimsIdentity"/>
/// (name, given name, surname, e-mail, user principal name, company, phones, culture and security identifier).
/// </summary>
[Export(typeof(IClaimProfileFiller)), Shared]
public class ClaimsProfileFiller : IClaimProfileFiller
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsProfileFiller"/> class.
    /// </summary>
    /// <param name="domainMapping">The options, named <c>DomainMapping</c>, mapping the domain part of a user principal name (or a <c>domain\user</c> name) to a short domain name.</param>
    public ClaimsProfileFiller(IOptionsMonitor<SimpleKeyValueSettings> domainMapping)
    {
        _domainMapping = domainMapping.Get("DomainMapping").Values;
    }

    private readonly IReadOnlyDictionary<string, string> _domainMapping;

    /// <summary>
    /// Builds the profile of the identity from its claims.
    /// </summary>
    /// <param name="identity">The identity, which must be a <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
    /// <returns>
    /// The profile. The account name and the domain are extracted from the user principal name; the culture is the invariant culture when the culture claim is missing, falls back to <c>en-GB</c>
    /// when the claim is not a valid culture name, and the security identifier to <c>S-1-0-0</c> when the sid claim is missing.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException"><paramref name="identity"/> is not a <see cref="System.Security.Claims.ClaimsIdentity"/>.</exception>
    public UserProfile GetProfile(IIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!(identity is ClaimsIdentity))
        {
            throw new NotSupportedException("Only identity from IClaimsIdentity are allowed.");
        }

        var claimsIdentity = (ClaimsIdentity)identity;

        // Create a UserProfile based on the identity received.
        var claimCulture = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, IdentityModel.Claims.ClaimTypes.Culture);

        CultureInfo culture;
        try
        {
            culture = new CultureInfo(claimCulture);
        }
        catch (Exception)
        {
            culture = new CultureInfo("en-GB");
        }
        var samAccountName = string.Empty;
        var domain = string.Empty;
        var name = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.Name, IdentityModel.Claims.ClaimTypes.Name);
        var surName = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.Surname, IdentityModel.Claims.ClaimTypes.Surname);
        var givenName = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.GivenName, IdentityModel.Claims.ClaimTypes.GivenName);

        var email = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.Email, IdentityModel.Claims.ClaimTypes.Email);
        var upn = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.Upn, IdentityModel.Claims.ClaimTypes.Upn);

        // transform the upn in domain\user.
        if (!string.IsNullOrWhiteSpace(upn))
        {
            if (upn.Contains('@'))
            {
                var array = upn.Split('@');
                if (array.Length == 2)
                {
                    samAccountName = array[0];
                    if (_domainMapping.ContainsKey(array[1]))
                    {
                        domain = _domainMapping[array[1]];
                    }
                    else
                    {
                        domain = array[1];
                        if (domain.Contains('.'))
                        {
                            domain = domain.Split('.')[0];
                        }
                    }
                }
            }
            else
            if (upn.Contains('\\'))
            {
                var array = upn.Split('\\');
                if (array.Length == 2)
                {
                    samAccountName = array[1];
                    if (_domainMapping.ContainsKey(array[0]))
                    {
                        domain = _domainMapping[array[0]];
                    }
                    else
                    {
                        domain = array[0];
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(samAccountName) ? $"{givenName} {surName}" : samAccountName;
        }

        var company = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, IdentityModel.Claims.ClaimTypes.Company);
        var phone = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.HomePhone);
        var mobile = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, ClaimTypes.MobilePhone);

        var sid = ClaimsProfileFiller.ExtractClaimValue(claimsIdentity.Claims, IdentityModel.Claims.ClaimTypes.Sid);
        sid = string.IsNullOrWhiteSpace(sid) ? "S-1-0-0" : sid;

        return new UserProfile(name,
                               email,
                               string.Empty,
                               company,
                               givenName,
                               surName,
                               sid,
                               string.Empty,
                               mobile,
                               phone,
                               string.Empty,
                               string.Empty,
                               upn,
                               string.Empty,
                               string.Empty,
                               string.Empty,
                               string.Empty,
                               samAccountName,
                               domain,
                               culture,
                               string.Empty,
                               string.Empty);
    }

    private static string ExtractClaimValue(IEnumerable<Claim> claims, params string[] claimTypes)
    {
        var claim = claims.FirstOrDefault(c => claimTypes.Any(t => t.Equals(c.Type, StringComparison.InvariantCultureIgnoreCase)));
        try
        {
            return null != claim ? claim.Value : string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

}
