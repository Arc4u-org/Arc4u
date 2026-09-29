using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Arc4u.Blazor;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using Blazored.LocalStorage;
using FluentResults;
using Microsoft.JSInterop;

namespace Arc4u.OAuth2.TokenProvider;

/// <summary>
/// Provides an implementation of <see cref="ITokenProvider"/> for Blazor applications.
/// </summary>
[Export(ProviderName, typeof(ITokenProvider))]
public class BlazorTokenProvider : ITokenProvider
{
    /// <summary>The key (<c>blazor</c>) under which the provider is registered.</summary>
    public const string ProviderName = "blazor";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorTokenProvider"/> class with the specified local storage service, JS runtime, and token window interop.
    /// </summary>
    /// <param name="localStorageService">The local storage service to use for storing and retrieving tokens.</param>
    /// <param name="jsRuntime">The JS runtime to use for interop calls.</param>
    /// <param name="windowInterop">The token window interop to use for opening a window.</param>
    public BlazorTokenProvider(ILocalStorageService localStorageService, IJSRuntime jsRuntime, ITokenWindowInterop windowInterop)
    {
        _localStorage = localStorageService;
        _jsRuntime = jsRuntime;
        _windowInterop = windowInterop;
    }

    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private readonly ITokenWindowInterop _windowInterop;

    /// <summary>
    /// Asynchronously retrieves an OAuth2 token.
    /// </summary>
    /// <param name="settings">The settings to be used for getting the token.</param>
    /// <param name="platformParameters">The platform-specific parameters, if any (not used in this implementation).</param>
    /// <returns>A task representing the asynchronous operation, containing the requested <see cref="TokenInfo"/> if successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the settings parameter or its Values property is null.</exception>
    /// <exception cref="UriFormatException">Thrown when the RedirectUrl from settings is not a valid URL.</exception>
    /// <exception cref="Exception">Thrown when no token is found after the window operation.</exception>
    public async Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.Values);

        var token = await GetToken().ConfigureAwait(false);

        if (null != token)
        {
            return token;
        }

        var authority = settings.Values.ContainsKey(TokenKeys.AuthorityKey) ? settings.Values[TokenKeys.AuthorityKey] : throw new ArgumentNullException(nameof(settings), TokenKeys.AuthorityKey);
        var redirectUrl = settings.Values.ContainsKey(TokenKeys.RedirectUrl) ? settings.Values[TokenKeys.RedirectUrl] : throw new ArgumentNullException(nameof(settings), TokenKeys.RedirectUrl);

        var result = Uri.TryCreate(redirectUrl, UriKind.Absolute, out var redirectUri);

        if (!result)
        {
            throw new UriFormatException($"RedirectUrl {redirectUrl} is not a valid url.");
        }

        var redirectTo = WebUtility.UrlEncode(redirectUri!.Authority + redirectUri.LocalPath.TrimEnd(new[] { '/' }));

        await _windowInterop.OpenWindowAsync(_jsRuntime, _localStorage, $"{authority}/redirectto/{redirectTo}").ConfigureAwait(false);

        return await GetToken().ConfigureAwait(false) ?? throw new InvalidOperationException("No token found!");
    }

    /// <summary>
    /// Asynchronously retrieves a stored OAuth2 token from local storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the requested <see cref="TokenInfo"/> if successful, null otherwise.</returns>
    private async Task<TokenInfo?> GetToken()
    {
        var accessToken = await _localStorage.GetItemAsStringAsync("token").ConfigureAwait(false);

        if (!string.IsNullOrEmpty(accessToken))
        {
            try
            {
                var token = new JwtSecurityToken(accessToken);
                if (token.ValidTo > DateTime.UtcNow.AddMinutes(-5))
                {
                    return new TokenInfo("Bearer", accessToken, token.ValidTo);
                }

                // ensure the token is removed before we try to have a new one.
                await _localStorage.RemoveItemAsync("token").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _localStorage.RemoveItemAsync("token").ConfigureAwait(false);
            }

        }

        return null;
    }

    /// <summary>
    /// Signs out the user by clearing the token.
    /// </summary>
    /// <param name="settings">The settings to be used for signing out (not used in this implementation).</param>
    /// <param name="cancellationToken">The Cancellation token <see cref="CancellationToken"/></param>
    /// <returns><see cref="ValueTask"/></returns>
    public async ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        await _localStorage.RemoveItemAsync("token", cancellationToken).ConfigureAwait(false);
    }
}
