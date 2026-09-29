---
description: "Sign the user of a desktop or mobile .NET app in with OpenID Connect and call APIs with the user's token; status of MSAL in Arc4u 9."
---
# Desktop and mobile clients

A desktop app (WPF, Windows Forms, console) or a .NET MAUI app signs its user in with OpenID Connect
in a browser, keeps the tokens, and sends the access token to the APIs it calls.
`Arc4u.OAuth2.Client.Authentication` does this with
[Duende.IdentityModel.OidcClient](https://www.nuget.org/packages/Duende.IdentityModel.OidcClient): it
registers the `OidcClient`, a [token provider](token-providers.md) that signs in, refreshes and
caches the tokens, and an `HttpClient` handler.

## MSAL status

Arc4u 9 does not ship an MSAL (Microsoft Authentication Library) package. The project that
wraps MSAL (package ID `Arc4u.OAuth2.Msal`, folder `src/Arc4u.OAuth.Msal`) still targets
`net8.0` only, is not part of the Arc4u 9 solution, and is not published for 9.x. The 9.0
roadmap ([#129](https://github.com/Arc4u-org/Arc4u/issues/129)) drops MSAL support and adds
IdentityModel for WPF and MAUI apps instead: that is `Arc4u.OAuth2.Client.Authentication`.
See [MSAL status](../../migration/8x-to-9.md#msal-status) in the migration guide and the
[package support](../package-support.md) page.

`BlazorMsalTokenProvider` in `Arc4u.OAuth2.Blazor` is not affected: it relies on the Microsoft
WebAssembly authentication, not on this package (see [Blazor](blazor.md)).

## Install

```bash
dotnet add package Arc4u.OAuth2.Client.Authentication --prerelease
```

The package references `Arc4u.OAuth2` and `Duende.IdentityModel.OidcClient`. You also need an
`IBrowser` implementation that shows the sign-in page: Duende provides samples for desktop apps
(a system browser with a loopback redirect), and MAUI has `WebAuthenticator`.

## Configuration

The configuration overload of `AddOidcClientAuthentication` reads the `Authentication` section
(pass `authenticationSectionName` to use another one) and registers the settings named
`OidcClient` (pass `settingsKey` to change it). The client ID and scopes are read from the section
whose full path is in the `OidcClientIdSettingsSectionPath` key of that section, by default
`Authentication:OidcClient.Settings`: this path does not follow `authenticationSectionName`.

```json
{
  "Authentication": {
    "DefaultAuthority": {
      "Url": "https://login.microsoftonline.com/<tenant-id>/v2.0"
    },
    "OidcClient.Settings": {
      "ClientId": "<client-id>",
      "Scopes": [ "openid", "profile", "offline_access", "api://inventory/access" ]
    },
    "CallbackPath": "http://127.0.0.1:7890/",
    "PostLogoutRedirectUri": "http://127.0.0.1:7890/"
  },
  "Application.Configuration": {
    "ApplicationName": "Inventory.Desktop",
    "Environment": {
      "Name": "Development",
      "LoggingName": "Inventory.Desktop",
      "TimeZone": "UTC"
    }
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Authentication:DefaultAuthority:Url` | `Uri` | none (required) | The authority. The metadata is read from `<Url>/.well-known/openid-configuration`, or from `MetaDataAddress`. |
| `Authentication:OidcClient.Settings:ClientId` | `string` | none (required) | Client ID of the app (a public client: no secret). |
| `Authentication:OidcClient.Settings:Scopes` | `string[]` | none (required) | Scopes requested. Add `offline_access` to get a refresh token. |
| `Authentication:OidcClient.Settings:ProviderId` | `string` | `OidcClientIdentityModel` | Key of the token provider. |
| `Authentication:CallbackPath` | `string` | `/` | Redirect URI registered for the app. |
| `Authentication:PostLogoutRedirectUri` | `string` | `/` | Redirect URI after sign-out. |
| `Authentication:LoadProfile` | `bool` | `false` | Loads the claims of the user info endpoint. |

`Application.Configuration` is read by `AddApplicationConfig` in the code below (see
[Configuration](../configuration/index.md)). HTTPS is required for the metadata, except when the
authority is `localhost`. The `Authentication` section also
takes the claims settings of the server authentication (`ClaimsIdentifier`, `DomainsMapping`,
`ClaimsMiddleWare:ClaimsFiller`); see [Server authentication](../authentication-server/index.md).

> [!NOTE]
> `ForceRefreshTimeoutTimeSpan`, `RefreshTokenLifetime`, `NameClaimType`, `RoleClaimType` and
> `ValidateAuthority` are bound but not used (known issue): the access token is refreshed only
> once expired, and the refresh token is assumed valid for 90 days. `ClockSkew` is not copied
> from the configuration: it stays at 5 minutes unless you use the overload that takes an
> `Action<OidcClientAuthenticationOptions>`.

## Register the services

```csharp
// Program.cs
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Client.Authentication.Cache;
using Arc4u.OAuth2.Client.Authentication.Token;
using Arc4u.OAuth2.Client.Authentication.TokenProvider;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddILogger();
builder.Services.AddApplicationConfig(builder.Configuration);
builder.Services.AddSingleton<IApplicationContext, ApplicationInstanceContext>();
builder.Services.AddSingleton<ISecureCache, CacheTokens>();

builder.Services.AddOidcClientAuthentication(new SystemBrowser(), NullLoggerFactory.Instance, builder.Configuration);
builder.Services.AddKeyedSingleton<ITokenProvider, OidcClientIdentityModelTokenProvider>(OidcClientIdentityModelTokenProvider.TokenProviderName);

builder.Services.AddHttpClient<InventoryClient>(client => client.BaseAddress = new Uri("https://inventory.example.com/"))
    .AddHttpMessageHandler(sp => new JwtHttpHandler<InventoryClient>(
        sp,
        sp.GetRequiredService<ILogger<InventoryClient>>(),
        sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("OidcClient")));

using var host = builder.Build();
```

`SystemBrowser` is your `IBrowser` implementation. `CacheTokens` names its files after the
environment name and logging name of `Application.Configuration`.

The first call through the handler signs the user in: <xref:Arc4u.OAuth2.Client.Authentication.TokenProvider.OidcClientIdentityModelTokenProvider>
returns the cached access token while it is valid, redeems the refresh token when the access token
is expired, and opens the browser when there is no valid refresh token. The tokens are kept in
the `ISecureCache`.

> [!CAUTION]
> <xref:Arc4u.OAuth2.Client.Authentication.Cache.CacheTokens> writes the tokens, refresh token
> included, as plain JSON files in the `OAuth2` folder of the user's local application data
> (`%LOCALAPPDATA%` on Windows, `~/.local/share` on Linux, `~/Library/Application Support` on
> macOS). The files get the default permissions of the process: on Linux and macOS they are
> typically readable by the other local users (`-rw-r--r--`) unless the home folder blocks access;
> on Windows the folder is protected by the user's access control list. Register your own
> `ISecureCache` that encrypts them (for example with the Windows Data Protection API, or
> `SecureStorage` in MAUI) when this is not acceptable.

The handler (<xref:Arc4u.OAuth2.Client.Authentication.Token.JwtHttpHandler`1>) needs the
`IApplicationContext` when it is created, and throws `ConfigurationException` without it. It uses
the token type as the scheme and, when there is a principal, adds a `culture` header and an
`activityid` header (when the context has an activity ID). Its constructor that takes a settings
name resolves a keyed `IKeyValueSettings`, which `AddOidcClientAuthentication` does not register:
pass the settings as above.

## Build the principal

<xref:Arc4u.OAuth2.Client.Security.Principal.AppPrincipalFactory> builds the `AppPrincipal` of the
user from the claims of the access token and of the claims filler, caches the claims in the
`ISecureCache` (to work offline), and sets the principal in the `IApplicationContext`. Call
`CreatePrincipalAsync(settings)` with the `OidcClient` settings at startup, and
`SignOutUserAsync(settings, cancellationToken)` to sign out. It needs an `INetworkInformation`
(`AlwaysConnected` when the app has no offline mode) and an `ICacheKeyGenerator`.

## Sign in with a user name and password

<xref:Arc4u.OAuth2.Token.UsernamePasswordTokenProvider> (key `usernamePassword`, package
`Arc4u.OAuth2.Client`) asks the user for credentials through your `IUserNamePasswordProvider`,
keeps them in the `ISecureCache`, and gets a token with the password grant through the
`CredentialDirect` provider. Prefer the browser sign-in above: the password grant does not support
multi-factor authentication.

> [!CAUTION]
> The user name **and the password** are stored in the same `ISecureCache` as the token. With
> `CacheTokens`, the password is written to disk in clear text. Use an `ISecureCache` that
> encrypts its values with this provider.

## Troubleshooting

### The call fails with an exception from the token provider

`OidcClientIdentityModelTokenProvider` throws when the refresh of the access token fails
(`Exception` with the error of the authority) and when the sign-in fails or is cancelled
(`AccessViolationException`). The desktop `JwtHttpHandler<T>` does not catch them: the call to the
API fails with the same exception. Catch it where you call the API, and sign the user in again.

### ConfigurationException: No settings found for OidcClient

You used the constructor of `JwtHttpHandler<T>` that takes a settings name. Pass the settings
themselves, as in [Register the services](#register-the-services). For the same reason,
`AppPrincipalFactory.CreatePrincipalAsync("OidcClient")` returns a failed result: pass the settings.

## See also

- [Token providers](token-providers.md)
- [HttpClient and gRPC clients](httpclient-grpc.md)
- [Migrate from Arc4u 8.x to 9](../../migration/8x-to-9.md#adal-and-protobuf-removed), for the ADAL removal
- <xref:Arc4u.OAuth2.Client.Authentication> in the API reference
