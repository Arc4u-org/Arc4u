using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.Blazor;

/// <summary>
/// Writes the signed-in user's authentication state into the page for the WebAssembly client with only the claims the client needs: the
/// name claim, the role claims, and the claim types the application lists. It replaces <c>SerializeAllClaims = true</c>, which sends every
/// claim of the user to the browser.
/// </summary>
/// <remarks>
/// <para>
/// Plug it into the serialization of the server project:
/// <c>.AddAuthenticationStateSerialization(o =&gt; o.SerializationCallback = AuthenticationStateClaimsSerializer.Create(ClaimTypes.Email))</c>.
/// </para>
/// <para>
/// The <see cref="Arc4uClaimTypes.SessionKey"/> claim (the key of the user's session in the cookie's session store) is never sent, even when
/// it is listed. Send <see cref="Arc4uClaimTypes.Authorization"/> only when the WebAssembly client evaluates authorizations itself: the
/// server is where they are enforced.
/// </para>
/// </remarks>
public static class AuthenticationStateClaimsSerializer
{
    // Claims that must never reach a browser, whatever the application lists.
    private static readonly HashSet<string> NeverSent = new(StringComparer.OrdinalIgnoreCase) { Arc4uClaimTypes.SessionKey };

    /// <summary>Creates a serialization callback that sends the name and role claims plus the claims of <paramref name="additionalClaimTypes"/>.</summary>
    /// <param name="additionalClaimTypes">The claim types sent besides the name and role claims.</param>
    /// <returns>The callback to set as <c>AuthenticationStateSerializationOptions.SerializationCallback</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="additionalClaimTypes"/> is <see langword="null"/>.</exception>
    public static Func<AuthenticationState, ValueTask<AuthenticationStateData?>> Create(params IEnumerable<string> additionalClaimTypes)
    {
        ArgumentNullException.ThrowIfNull(additionalClaimTypes);

        var allowList = additionalClaimTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return state => SerializeAsync(state, allowList);
    }

    /// <summary>
    /// Builds the authentication state data: nothing for an anonymous user; otherwise the first identity's name and role claim types, the first
    /// name claim, every role claim, and every claim whose type is in <paramref name="additionalClaimTypes"/>, except the claims that never
    /// leave the server.
    /// </summary>
    /// <param name="authenticationState">The authentication state of the user.</param>
    /// <param name="additionalClaimTypes">The claim types sent besides the name and role claims.</param>
    /// <returns>The data written into the page, or <see langword="null"/> for an anonymous user.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="authenticationState"/> or <paramref name="additionalClaimTypes"/> is <see langword="null"/>.</exception>
    public static ValueTask<AuthenticationStateData?> SerializeAsync(AuthenticationState authenticationState, IEnumerable<string> additionalClaimTypes)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);
        ArgumentNullException.ThrowIfNull(additionalClaimTypes);

        var user = authenticationState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return ValueTask.FromResult<AuthenticationStateData?>(null);
        }

        var data = new AuthenticationStateData();
        if (user.Identities.FirstOrDefault() is { } identity)
        {
            data.NameClaimType = identity.NameClaimType;
            data.RoleClaimType = identity.RoleClaimType;
        }

        if (!NeverSent.Contains(data.NameClaimType) && user.FindFirst(data.NameClaimType) is { } nameClaim)
        {
            data.Claims.Add(new ClaimData(nameClaim));
        }

        if (!NeverSent.Contains(data.RoleClaimType))
        {
            foreach (var roleClaim in user.FindAll(data.RoleClaimType))
            {
                data.Claims.Add(new ClaimData(roleClaim));
            }
        }

        var alreadySent = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { data.NameClaimType, data.RoleClaimType };
        foreach (var claimType in additionalClaimTypes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(claimType) || alreadySent.Contains(claimType) || NeverSent.Contains(claimType))
            {
                continue;
            }

            foreach (var claim in user.FindAll(claimType))
            {
                data.Claims.Add(new ClaimData(claim));
            }
        }

        return ValueTask.FromResult<AuthenticationStateData?>(data);
    }
}
