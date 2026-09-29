using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using FluentResults;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>
/// Provides an implementation of <see cref="ITokenProvider"/> for Blazor applications using MSAL.
/// </summary>
[Export(ProviderName, typeof(ITokenProvider))]
public class BlazorMsalTokenProvider : ITokenProvider
{
    /// <summary>The key (<c>blazor</c>) under which the provider is registered.</summary>
    public const string ProviderName = "blazor";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorMsalTokenProvider"/> class with the specified access token provider accessor and application context.
    /// </summary>
    /// <param name="accessor">The access token provider accessor to use for obtaining access tokens.</param>
    /// <param name="applicationContext">The application context to use for getting the principal.</param>
    public BlazorMsalTokenProvider(IAccessTokenProviderAccessor accessor, IApplicationContext applicationContext)
    {
        _accessor = accessor;
        _applicationContext = applicationContext;
    }

    private readonly IAccessTokenProviderAccessor _accessor;
    private readonly IApplicationContext _applicationContext;

    /// <summary>
    /// Asynchronously retrieves an OAuth2 token.
    /// </summary>
    /// <param name="settings">The settings to be used for getting the token.</param>
    /// <param name="platformParameters">The platform-specific parameters, if any (not used in this implementation).</param>
    /// <returns>A task representing the asynchronous operation, containing the requested <see cref="TokenInfo"/> if successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the settings parameter is null.</exception>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
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

        return new Error("No token found!");
    }

    /// <summary>
    /// Signs out the user by clearing the token.
    /// </summary>
    /// <param name="settings">The settings to be used for signing out (not used in this implementation).</param>
    /// <param name="cancellationToken">The Cancellation token <see cref="CancellationToken"/></param>
    /// <returns><see cref="ValueTask"/></returns>
    public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
