using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>
/// Provides an implementation of <see cref="ITokenProvider"/> for Blazor applications using MSAL.
/// </summary>
[Export(ProviderName, typeof(ITokenProvider))]
public class BlazorMsalTokenProvider : ISignOutTokenProvider
{
    /// <summary>The key (<c>blazor</c>) under which the provider is registered.</summary>
    public const string ProviderName = "blazor";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorMsalTokenProvider"/> class with the specified access token provider accessor, application context and navigation manager.
    /// </summary>
    /// <param name="accessor">The access token provider accessor to use for obtaining access tokens.</param>
    /// <param name="applicationContext">The application context to use for getting the principal.</param>
    /// <param name="navigation">The navigation manager used to redirect the user to the login page when the token cannot be obtained silently.</param>
    public BlazorMsalTokenProvider(IAccessTokenProviderAccessor accessor, IApplicationContext applicationContext, NavigationManager navigation)
    {
        _accessor = accessor;
        _applicationContext = applicationContext;
        _navigation = navigation;
    }

    private readonly IAccessTokenProviderAccessor _accessor;
    private readonly IApplicationContext _applicationContext;
    private readonly NavigationManager _navigation;

    /// <summary>
    /// Asynchronously retrieves an OAuth2 token.
    /// </summary>
    /// <param name="settings">The settings to be used for getting the token.</param>
    /// <param name="platformParameters">The platform-specific parameters, if any (not used in this implementation).</param>
    /// <returns>A task representing the asynchronous operation. The token is the one stored in the identity of the current principal when it is still valid, otherwise the one requested to the MSAL access token provider; a failed result is returned when no token is found.
    /// When the MSAL access token provider requires an interactive login, the current principal is cleared and the user is redirected to the login page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the settings parameter is null.</exception>
    public async ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (null != _applicationContext.Principal)
        {
            var identity = (ClaimsIdentity)_applicationContext.Principal.Identity!;

            if (!string.IsNullOrWhiteSpace(identity.BootstrapContext?.ToString()))
            {
                var token = identity.BootstrapContext.ToString();

                JwtSecurityToken jwt = new(token);

                if (jwt.ValidTo > DateTime.UtcNow)
                {
                    return new TokenInfo("Bearer", token!, jwt.ValidTo).ToResult();
                }
            }

        }

        var requestAccessToken = await _accessor.TokenProvider.RequestAccessToken().ConfigureAwait(false);

        if (requestAccessToken.TryGetToken(out var accessToken))
        {
            var token = accessToken.Value;

            JwtSecurityToken jwt = new(token);
            return new TokenInfo("Bearer", token, jwt.ValidTo).ToResult();
        }

        if (requestAccessToken.Status == AccessTokenResultStatus.RequiresRedirect && !string.IsNullOrEmpty(requestAccessToken.InteractiveRequestUrl))
        {
            // The principal holds a token that can no longer be refreshed: clear it so it is rebuilt after the login.
            _applicationContext.SetPrincipal(null);

            if (requestAccessToken.InteractionOptions is not null)
            {
                _navigation.NavigateToLogin(requestAccessToken.InteractiveRequestUrl, requestAccessToken.InteractionOptions);
            }
            else
            {
                _navigation.NavigateToLogin(requestAccessToken.InteractiveRequestUrl);
            }

            return new Error("The token cannot be obtained silently, the user is redirected to the login page.");
        }

        return new Error("No token found!");
    }

    /// <summary>
    /// Clears the current principal, so its token (the <see cref="ClaimsIdentity.BootstrapContext"/>) is no longer used.
    /// The MSAL tokens are managed by the authentication of the Blazor application: the application signs out with <c>NavigateToLogout</c>.
    /// </summary>
    /// <param name="settings">The settings, which are not used.</param>
    /// <param name="cancellationToken">The cancellation token, which is not used.</param>
    /// <returns>A completed <see cref="ValueTask"/>.</returns>
    public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        _applicationContext.SetPrincipal(null);

        return ValueTask.CompletedTask;
    }
}
