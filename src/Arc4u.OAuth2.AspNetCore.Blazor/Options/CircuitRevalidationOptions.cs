using Microsoft.AspNetCore.Authentication.Cookies;

namespace Arc4u.Blazor;

/// <summary>
/// Configures how an open Blazor circuit checks that the sign-in it started with still exists (see
/// <see cref="AppPrincipalServerAuthenticationStateProvider"/>).
/// </summary>
public sealed class CircuitRevalidationOptions
{
    /// <summary>The default configuration section: <c>Authentication:CircuitRevalidation</c>.</summary>
    public const string DefaultSectionName = "Authentication:CircuitRevalidation";

    /// <summary>The longest <see cref="Interval"/> accepted: a longer one would let a signed-out tab act as the user for longer than a session lasts.</summary>
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets a value indicating whether the sign-in of an open circuit is checked. The default is <see langword="true"/>. Turned off,
    /// a circuit keeps its user until it reconnects, even after the user signed out in another tab.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how long a circuit may keep acting as a user whose sign-in has ended (sign-out in another tab, expired session) before
    /// it notices. Each check is one read of the session store per open circuit. The default is two minutes; it must be more than zero and
    /// at most <see cref="MaxInterval"/>.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the cookie authentication scheme whose session store holds the sign-ins. The default is
    /// <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/>, the one Arc4u's OpenID Connect authentication uses.
    /// </summary>
    public string CookieScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
}
