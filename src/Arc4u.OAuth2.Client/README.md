# Arc4u.OAuth2.Client

Builds the `AppPrincipal` of the user of a client application (desktop, mobile, Blazor) from the token of an Arc4u token provider, and provides the user name and password token provider.

## Install

```bash
dotnet add package Arc4u.OAuth2.Client --prerelease
```

## Usage

```csharp
builder.Services.AddSingleton<IAppPrincipalFactory, AppPrincipalFactory>();

// Once the host is built: sign the user in and set the principal in the IApplicationContext.
var settings = host.Services.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("OidcClient");
var principal = await host.Services.GetRequiredService<IAppPrincipalFactory>().CreatePrincipalAsync(settings);
```

`AppPrincipalFactory` asks the token provider named by the `ProviderId` of the settings for a token, and builds the principal from its claims.

## Documentation

- Guide: [Desktop and mobile clients](https://arc4u-org.github.io/Arc4u/guides/authentication-client/desktop.html)
- API reference: [Arc4u.OAuth2.Client](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.Client.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
