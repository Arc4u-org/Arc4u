using System.Diagnostics;
using Arc4u.Dependency.Attribute;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace Arc4u.Blazor;

/// <summary>
/// Provides a mechanism to interact with an OAuth token window.
/// </summary>
[Export(typeof(ITokenWindowInterop)), Shared]
public class WindowInterop : ITokenWindowInterop
{
    // How long to wait for the popup to signal that the token was fetched. Internal so the tests can shorten it.
    internal TimeSpan LoginTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Opens a new window asynchronously with the specified URL and features, using the provided JS runtime and local storage service.
    /// Also, it handles the local storage entries for "token" and "fetching" before opening the window.
    /// The method waits for the "fetching" entry to be set in local storage, signaling the completion of token fetching, before returning.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime to use for opening the window.</param>
    /// <param name="localStorage">The local storage service to use for storing and retrieving tokens.</param>
    /// <param name="url">The URL to open in the new window.</param>
    /// <param name="feature">The features of the new window. Defaults to "toolbar=no,scrollbars=yes,resizable=yes,top=500,left=500,width=450,height=500".</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">The popup could not be opened, typically because <c>_content/Arc4u.OAuth2.Blazor/CreatePopupWindow.js</c> is not referenced by the application.</exception>
    /// <exception cref="TimeoutException">The login was not completed within 5 minutes, for example because the popup was closed.</exception>
    public async Task OpenWindowAsync(IJSRuntime jsRuntime, ILocalStorageService localStorage, string url, string feature = "toolbar=no,scrollbars=yes,resizable=yes,top=500,left=500,width=450,height=500")
    {
        // Virtual and non static for Mocking purpose.

        if (await localStorage.ContainKeyAsync("token").ConfigureAwait(false))
        {
            await localStorage.RemoveItemAsync("token").ConfigureAwait(false);
        }

        if (await localStorage.ContainKeyAsync("fetching").ConfigureAwait(false))
        {
            await localStorage.RemoveItemAsync("fetching").ConfigureAwait(false);
        }

        try
        {
            await jsRuntime.InvokeVoidAsync("Arc4u.CreatePopupWindow", url, feature).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException("Could not open the login popup. Check that _content/Arc4u.OAuth2.Blazor/CreatePopupWindow.js is referenced in index.html.", ex);
        }

        // Without a limit, a popup closed before the login completes would leave the caller waiting forever.
        var elapsed = Stopwatch.StartNew();
        do
        {
            if (elapsed.Elapsed > LoginTimeout)
            {
                throw new TimeoutException($"The login was not completed within {LoginTimeout}.");
            }

            await Task.Delay(100).ConfigureAwait(false);
        }
        while (null == await localStorage.GetItemAsync<string>("fetching").ConfigureAwait(false));

        await localStorage.RemoveItemAsync("fetching").ConfigureAwait(false);
    }
}
