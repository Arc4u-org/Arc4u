# Arc4u.OAuth2.Blazor

Authentication for Blazor WebAssembly apps: gets the user's access token from the Blazor server with the authentication cookie, sends it to APIs, and builds the Arc4u `AppPrincipal` from the authentication state.

## Install

```bash
dotnet add package Arc4u.OAuth2.Blazor --prerelease
```

## Usage

```csharp
builder.Services.AddTransient<AttachCookiesHandler>();
builder.Services.AddAuthenticationCookie(builder.Configuration);
builder.Services.AddKeyedSingleton<ITokenProvider, ClientTokenProvider>(ClientTokenProvider.ProviderName);

builder.Services.AddHttpClient<InventoryClient>()
    .AddHttpMessageHandler(sp => new JwtHttpHandler(sp,
        sp.GetRequiredService<ILogger<JwtHttpHandler>>(),
        sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("OAuth2")));
```

`AddAuthenticationCookie` reads the `Authentication:OAuth2.Settings` section (`BaseUri` and `TokenRequestUrl` of the server endpoint that returns the token).

## Expired tokens with MSAL

With `BlazorMsalTokenProvider` (key `blazor`), the token of the current principal is used while it is valid, otherwise a new one is requested from the MSAL `IAccessTokenProvider`. When MSAL cannot renew it silently (`AccessTokenResultStatus.RequiresRedirect`, for example once the refresh token has expired), the provider:

1. clears the current principal, so it is rebuilt from the new token after the login;
2. redirects the user to the login page with `NavigationManager.NavigateToLogin`, passing the interaction options returned by MSAL;
3. returns a failed result: `JwtHttpHandler` logs it and sends the pending request without token.

The user comes back to the application once logged in, instead of getting `401 Unauthorized` until the browser is restarted.

## Documentation

- Guide: [Blazor](https://arc4u-org.github.io/Arc4u/guides/authentication-client/blazor.html)
- API reference: [Arc4u.Blazor](https://arc4u-org.github.io/Arc4u/api/Arc4u.Blazor.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
