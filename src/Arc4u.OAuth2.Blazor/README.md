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

## Documentation

- Guide: [Blazor](https://arc4u-org.github.io/Arc4u/guides/authentication-client/blazor.html)
- API reference: [Arc4u.Blazor](https://arc4u-org.github.io/Arc4u/api/Arc4u.Blazor.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
