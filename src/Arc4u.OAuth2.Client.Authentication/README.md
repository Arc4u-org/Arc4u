# Arc4u.OAuth2.Client.Authentication

Signs the user of a desktop or mobile .NET app in with OpenID Connect (Duende.IdentityModel.OidcClient) and sends the user's access token to the APIs the app calls.

## Install

```bash
dotnet add package Arc4u.OAuth2.Client.Authentication --prerelease
```

## Usage

```csharp
builder.Services.AddOidcClientAuthentication(browser, loggerFactory, builder.Configuration);
builder.Services.AddKeyedSingleton<ITokenProvider, OidcClientIdentityModelTokenProvider>(OidcClientIdentityModelTokenProvider.TokenProviderName);

builder.Services.AddHttpClient<InventoryClient>()
    .AddHttpMessageHandler(sp => new JwtHttpHandler<InventoryClient>(sp,
        sp.GetRequiredService<ILogger<InventoryClient>>(),
        sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("OidcClient")));
```

`AddOidcClientAuthentication` reads the `Authentication` and `Authentication:OidcClient.Settings` sections; `browser` is your `IBrowser` implementation.

## Documentation

- Guide: [Desktop and mobile clients](https://arc4u-org.github.io/Arc4u/guides/authentication-client/desktop.html)
- API reference: [Arc4u.OAuth2.Client.Authentication](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.Client.Authentication.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
