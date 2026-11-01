# Arc4u.OAuth2.Blazor

Authentication for the client (WebAssembly) project of a Blazor Web App, whose server signs the user in with a cookie: gets the user's access token from the server with that cookie, sends it to APIs, and builds the Arc4u `AppPrincipal` from the authentication state sent by the server.

Standalone Blazor WebAssembly apps, which sign the user in themselves, are not supported.

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

`ConfigureAuthStateDeserializationOptions` and `AppPrincipalFromAuthenticationState` rebuild the `AppPrincipal` from the authentication state that the server serializes with `AddAuthenticationStateSerialization`:

```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddSingleton<IAppPrincipalAuthenticationStateProvider, AppPrincipalFromAuthenticationState>();
builder.Services.AddSingleton<IConfigureOptions<AuthenticationStateDeserializationOptions>, ConfigureAuthStateDeserializationOptions>();
```

## Documentation

- Guide: [Blazor](https://arc4u-org.github.io/Arc4u/guides/authentication-client/blazor.html)
- API reference: [Arc4u.Blazor](https://arc4u-org.github.io/Arc4u/api/Arc4u.Blazor.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
