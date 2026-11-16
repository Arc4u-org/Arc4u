---
description: "Choose and configure the token provider that gets the token for a downstream call: the user's token, on-behalf-of, client credentials, user name and password, or a remote secret."
---
# Token providers

A token provider gets the token that a call to another API carries. This page lists the
providers Arc4u ships, explains how a handler picks one, and shows how to configure the
scenarios of an ASP.NET Core back end: forward the user's token, exchange it on behalf of the
user, or use an application identity. The concept is summarized in the
[glossary](../../concepts/glossary.md#token-provider); how the token is attached to the call is
in [HttpClient and gRPC clients](httpclient-grpc.md).

## How a provider is selected

Three pieces take part in every call:

- **Named settings**: a <xref:Arc4u.Configuration.SimpleKeyValueSettings> registered as a
  named option (read it with `IOptionsMonitor<SimpleKeyValueSettings>.Get(name)`). The key
  `ProviderId` names the provider, `AuthenticationType` says for which caller the settings
  apply, and the other keys (`ClientId`, `Scope`, ...) are read by the provider. The key names
  are the constants of <xref:Arc4u.OAuth2.Token.TokenKeys>.
- **A token provider**: an <xref:Arc4u.OAuth2.Token.ITokenProvider> registered as a keyed
  service, whose key is the `ProviderId` value.
- **A handler**: an HttpClient `DelegatingHandler` or a gRPC interceptor built with the
  settings. For each call it resolves the keyed provider, calls
  <xref:Arc4u.OAuth2.Token.ITokenProvider.GetTokenAsync*> with the settings, and writes the
  header.

```mermaid
flowchart LR
    Config["appsettings.json (Authentication:...)"] -->|"AddClientTokens, AddOnBehalfOf, ..."| Settings["Named SimpleKeyValueSettings"]
    Settings --> Handler["JwtHttpHandler / OAuth2Interceptor"]
    Handler -->|"keyed by ProviderId"| Provider["ITokenProvider"]
    Provider -->|"TokenInfo"| Handler
    Handler -->|"Authorization header"| Api["Downstream API"]
```

`AuthenticationType` decides whose token is sent:

- `Inject`: the provider's token is sent whoever the caller is. The application identity
  scenarios (client credentials, user name and password, remote secret) use it by default.
  The token type returned by the provider becomes the scheme: `Bearer` and `Basic` go in the
  `Authorization` header, any other value is used as the name of a custom header.
- Any other value: no token is sent when the current user was authenticated with another
  authentication type, and the scheme is always `Bearer`. The settings that Arc4u registers
  for the user's own token use `OAuth2` (bearer token of an API call) and `Cookies` (user signed
  in with OpenID Connect). The HttpClient handler checks that the settings value contains the
  identity's authentication type; the gRPC interceptor checks that they are equal. See
  [HttpClient and gRPC clients](httpclient-grpc.md#how-the-handlers-decide).

## Built-in token providers

Each provider exposes its key as a `ProviderName` constant (`TokenProviderName` for
`OidcClientIdentityModelTokenProvider`). Register the ones you use as keyed services under that
key, as shown in the scenarios below.

| Key | Type | Package | Token it returns |
|---|---|---|---|
| `Bootstrap` | <xref:Arc4u.OAuth2.TokenProviders.BootstrapContextTokenProvider> | `Arc4u.OAuth2.AspNetCore.Authentication` | The access token of the incoming request, kept in the `BootstrapContext` of the current identity. Fails when it is expired. |
| `Oidc` | <xref:Arc4u.OAuth2.TokenProviders.OidcTokenProvider> | `Arc4u.OAuth2.AspNetCore.Authentication` | The access token of a user signed in with OpenID Connect and a cookie, refreshed with the refresh token when it is about to expire. |
| `Obo` | <xref:Arc4u.OAuth2.TokenProviders.AzureADOboTokenProvider> | `Arc4u.OAuth2.AspNetCore.Authentication` | A token for another API, exchanged for the user's token with the on-behalf-of grant. |
| `ClientCredentials` | <xref:Arc4u.OAuth2.TokenProvider.ClientCredentialsTokenProvider> | `Arc4u.OAuth2.AspNetCore` | An application token (client credentials grant). |
| `Credential` | <xref:Arc4u.OAuth2.TokenProvider.CredentialTokenCacheTokenProvider> | `Arc4u.OAuth2.AspNetCore` | A token for a fixed user name and password (password grant), requested through the `CredentialDirect` <xref:Arc4u.OAuth2.TokenProvider.CredentialTokenProvider>. |
| `RemoteSecret` | <xref:Arc4u.OAuth2.TokenProvider.RemoteClientSecretTokenProvider> | `Arc4u.OAuth2.AspNetCore` | No call to an identity provider: the configured secret, sent in the configured header. |
| `Client` | <xref:Arc4u.OAuth2.TokenProvider.ClientTokenProvider> | `Arc4u.OAuth2.Blazor` | The user's token, fetched by a Blazor WebAssembly app from its server. See [Blazor](blazor.md). |
| `OidcClientIdentityModel` | <xref:Arc4u.OAuth2.Client.Authentication.TokenProvider.OidcClientIdentityModelTokenProvider> | `Arc4u.OAuth2.Client.Authentication` | The user's token in a desktop or mobile app. See [Desktop and mobile clients](desktop.md). |
| `usernamePassword` | <xref:Arc4u.OAuth2.Token.UsernamePasswordTokenProvider> | `Arc4u.OAuth2.Client` | A token for credentials that the user of a client app enters. See [Desktop and mobile clients](desktop.md). |

The table below shows which registration method produces the settings each provider reads.

| Scenario | Registration method | Section | Settings name | `ProviderId` | `AuthenticationType` |
|---|---|---|---|---|---|
| [Forward the user's token (API)](#forward-the-users-token) | `ConfigureOAuth2Settings`, called by `AddJwtAuthentication` and `AddHybridAuthentication` | `Authentication:OAuth2.Settings` | `OAuth2` | `Bootstrap` | `OAuth2` |
| [Forward the user's token (web app)](#forward-the-users-token) | `ConfigureOpenIdSettings`, called by `AddOidcAuthentication` and `AddHybridAuthentication` | `Authentication:OpenId.Settings` | `Cookies` | `Oidc` | `Cookies` |
| [On-behalf-of](#call-an-api-on-behalf-of-the-user) | `AddOnBehalfOf` | `Authentication:OnBehalfOf` | the entry name | `Obo` | `Inject` |
| [Client credentials](#call-an-api-with-the-application-identity) | `AddClientTokens` | `Authentication:ClientTokens` | the entry name | `ClientCredentials` | `Inject` |
| [User name and password](#call-an-api-with-a-user-name-and-password) | `AddClientTokens` | `Authentication:ClientTokens` | the entry name | `Credential` | `Inject` |
| [Remote secret](#send-a-remote-secret) | `AddRemoteSecretsAuthentication` | `Authentication:RemoteSecrets` | the entry name | `RemoteSecret` | `Inject` |

The `ProviderId` and `AuthenticationType` columns are the defaults; most sections let you
override them. The server-side registration methods and their sections are described in
[Server authentication](../authentication-server/index.md).

## Forward the user's token

Use it when an API calls another API that accepts the same token (same audience), or when a
web app calls its own API with the token of the signed-in user.

`AddJwtAuthentication` registers the settings named `OAuth2` and `AddOidcAuthentication`
registers the settings named `Cookies`; `AddHybridAuthentication` registers both. Register the
matching provider, then build the handler with those settings:

```csharp
// Program.cs
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProviders;

var builder = WebApplication.CreateBuilder(args);

// ... authentication: see the Server authentication guide.

builder.Services.AddKeyedScoped<ITokenProvider, BootstrapContextTokenProvider>(BootstrapContextTokenProvider.ProviderName);
builder.Services.AddKeyedScoped<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);

var app = builder.Build();
app.Run();
```

`OidcTokenProvider` also needs an `ITokenRefreshProvider`:
<xref:Arc4u.OAuth2.TokenProviders.RefreshTokenProvider> redeems the refresh token with the client
ID, client secret and authority of the OpenID Connect scheme.

Pass the settings named `OAuth2` to one handler and the settings named `Cookies` to another, and
chain them (see [Chain several handlers](httpclient-grpc.md#chain-several-handlers)): each one
sends the token only when the caller used its authentication type.

## Call an API on behalf of the user

The on-behalf-of flow exchanges the user's token for a token whose audience is the downstream
API. Use it when the downstream API does not accept the token your API received.
<xref:Arc4u.OAuth2.TokenProviders.AzureADOboTokenProvider> posts the exchange
(`grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer`, `requested_token_use=on_behalf_of`)
to the token endpoint of the `Default` authority, and caches the result until it expires and at most
the `MaxTime` of its token cache (see [Choose the token cache of an entry](#choose-the-token-cache-of-an-entry));
a token valid for one minute or less is not cached. The user's token is the access token kept by the cookie authentication or, when there is none, the
`BootstrapContext` of the current identity (the bearer token of the request). The cookie token
comes first because `UseOpenIdBearerInjector()` replaces the `BootstrapContext` with the
on-behalf-of token it injects.

```json
{
  "Authentication": {
    "DefaultAuthority": {
      "Url": "https://login.microsoftonline.com/<tenant-id>/v2.0"
    },
    "OnBehalfOf": {
      "Inventory": {
        "ClientId": "<client-id>",
        "ClientSecret": "<client-secret>",
        "Scopes": [ "api://inventory/.default" ],
        "AuthenticationType": "OAuth2"
      }
    }
  }
}
```

`AddOnBehalfOf(builder.Configuration)` reads `Authentication:OnBehalfOf` (pass `sectionName` to
use another section) and registers one settings entry per child, named after the child
(`Inventory` here). The configuration based `AddOidcAuthentication` and `AddHybridAuthentication`
already call it with the default section: do not call it a second time, see
[Troubleshooting](index.md#an-item-with-the-same-key-has-already-been-added).
`AddOnBehalfOfSettings(options, optionKey)` registers one entry from code.

| Key | Type | Default | Description |
|---|---|---|---|
| `Authentication:OnBehalfOf:<name>:ClientId` | `string` | none (required) | Client ID of your application. |
| `Authentication:OnBehalfOf:<name>:ClientSecret` | `string` | none (required) | Client secret of your application. |
| `Authentication:OnBehalfOf:<name>:Scopes` | `string[]` | none (required) | Scopes requested for the downstream API. |
| `Authentication:OnBehalfOf:<name>:AuthenticationType` | `string` | `Inject` | See [How a provider is selected](#how-a-provider-is-selected). |
| `Authentication:OnBehalfOf:<name>:ProviderId` | `string` | `Obo` | Key of the token provider. |
| `Authentication:OnBehalfOf:<name>:HttpClientName` | `string` | none | Name of the `HttpClient` of the `IHttpClientFactory` that calls the token endpoint. When empty, the default client is used. |
| `Authentication:OnBehalfOf:<name>:TokenCache` | object | `Authentication:OnBehalfOfCache`, then `Authentication:TokenCache` | `CacheName` and `MaxTime` of the cache that keeps the tokens of this entry. See [Choose the token cache of an entry](#choose-the-token-cache-of-an-entry). |

> [!NOTE]
> The provider returns the token type `Bearer`: with the default `Inject`, the token is sent in
> the `Authorization: Bearer` header. Set `AuthenticationType` to `OAuth2` (in an API) or
> `Cookies` (in a web app) to send the token only when the caller was authenticated that way.

Register the provider and the services it uses: the scoped `TokenRefreshInfo` (already registered
by `AddOidcAuthentication`, not by `AddJwtAuthentication`), an `ICacheHelper` that gives the cache
for the exchanged tokens (see [Caching](../caching/index.md)) and an `IActivitySourceFactory`.

```csharp
builder.Services.AddOnBehalfOf(builder.Configuration);
builder.Services.AddScoped<TokenRefreshInfo>();
builder.Services.AddKeyedScoped<ITokenProvider, AzureADOboTokenProvider>(AzureADOboTokenProvider.ProviderName);
```

The provider calls the token endpoint with the client named by `HttpClientName`, or the default
client, of the `IHttpClientFactory` when one is registered (`AddHttpClient`), and otherwise with a
shared `HttpClient`. It does not send the Windows credentials of the process: configure them, or a
proxy, on a named client and give its name in `HttpClientName` if your token endpoint requires them. A failure of the token endpoint, or a missing `ClientId`, `ClientSecret` or `Scopes`,
gives a failed result, not an exception.

## Call an API with the application identity

Use the client credentials grant for calls that do not depend on a user: background jobs,
calls to a technical API, or when the downstream API authorizes your application rather than
the user. Declare one entry per downstream API in `Authentication:ClientTokens`:

```json
{
  "Authentication": {
    "DefaultAuthority": {
      "Url": "https://login.microsoftonline.com/<tenant-id>/v2.0"
    },
    "ClientTokens": {
      "Inventory": {
        "Scenario": "ClientCredentials",
        "Scopes": [ "api://inventory/.default" ],
        "Settings": {
          "ClientId": "<client-id>",
          "ClientSecret": "<client-secret>"
        }
      }
    }
  }
}
```

```csharp
builder.Services.AddDefaultAuthority(builder.Configuration);
builder.Services.AddClientTokens(builder.Configuration);
builder.Services.AddKeyedSingleton<ITokenProvider, ClientCredentialsTokenProvider>(ClientCredentialsTokenProvider.ProviderName);
```

`AddClientTokens` reads `Authentication:ClientTokens` (pass `sectionName` to use another
section), validates every entry and throws a `ConfigurationException` at registration when one is
invalid. Each entry becomes the settings named after it (`Inventory` here). The configuration
based `AddJwtAuthentication` already calls it; calling it again is harmless, an entry is
registered only once.

<xref:Arc4u.OAuth2.TokenProvider.ClientCredentialsTokenProvider> sends the client ID and secret
in an `Authorization: Basic` header (`client_secret_basic`), and the scope and any extra
parameter in the form body. It caches the token in the registered `ITokenCache` (see the
[shortest setup](index.md#code) for its registration) and requests a new one when the cached token
expires in less than one minute.

| Key | Type | Default | Description |
|---|---|---|---|
| `Authentication:ClientTokens:<name>:Scenario` | `string` | none (required) | `ClientCredentials`, `UserPassword` or `Basic`. |
| `Authentication:ClientTokens:<name>:Scopes` | `string[]` | `openid` | Scopes requested, joined with spaces. |
| `Authentication:ClientTokens:<name>:AuthenticationType` | `string` | `Inject` | See [How a provider is selected](#how-a-provider-is-selected). |
| `Authentication:ClientTokens:<name>:Authority` | object | the `Default` authority | An authority for this entry only (`Url`, `TokenEndpoint`, `Issuer`, `MetaDataAddress`), registered under the entry name. |
| `Authentication:ClientTokens:<name>:TokenCache` | object | `Authentication:ClientTokensCache`, then `Authentication:TokenCache` | `CacheName` and `MaxTime` of the cache that keeps the tokens of this entry. See [Choose the token cache of an entry](#choose-the-token-cache-of-an-entry). |
| `Authentication:ClientTokens:<name>:Settings:ClientId` | `string` | none (required) | Client ID of your application. |
| `Authentication:ClientTokens:<name>:Settings:ClientSecret` | `string` | none (required) | Client secret of your application. |
| `Authentication:ClientTokens:<name>:Settings:<other>` | `string` | none | Any other key is sent to the token endpoint as an extra form parameter, for example `resource`. See [Cache keys](#cache-keys). |

The `Default` authority comes from `Authentication:DefaultAuthority`, read by
`AddDefaultAuthority`. When `TokenEndpoint` is not set, the token endpoint is read from the
OpenID Connect metadata of the authority (`<Url>/.well-known/openid-configuration`, or
`MetaDataAddress`).

## Call an API with a user name and password

Some identity providers issue a token for a technical account with the password grant. The
`UserPassword` scenario takes the user name and the password as two keys, the `Basic` scenario
takes them as one `user:password` value:

```json
{
  "Authentication": {
    "DefaultAuthority": {
      "Url": "https://sts.example.com/realms/example"
    },
    "ClientTokens": {
      "Reporting": {
        "Scenario": "UserPassword",
        "Scopes": [ "openid" ],
        "Settings": {
          "ClientId": "<client-id>",
          "User": "<user>",
          "Password": "<password>"
        }
      },
      "Archive": {
        "Scenario": "Basic",
        "Settings": {
          "ClientId": "<client-id>",
          "Credential": "<user>:<password>"
        }
      }
    }
  }
}
```

Both scenarios use the `Credential` provider, which caches the token and asks the `CredentialDirect`
provider for a new one when it expires in less than one minute. `ClientSecret` is optional in the
`Settings` of both scenarios, for identity providers that require it with the password grant.
Unlike the client credentials scenario, other `Settings` keys are not sent to the token endpoint.

```csharp
builder.Services.AddDefaultAuthority(builder.Configuration);
builder.Services.AddClientTokens(builder.Configuration);
builder.Services.AddKeyedTransient<ITokenProvider, CredentialTokenCacheTokenProvider>(CredentialTokenCacheTokenProvider.ProviderName);
builder.Services.AddKeyedSingleton<ICredentialTokenProvider, CredentialTokenProvider>(CredentialTokenProvider.ProviderName);
```

`CredentialTokenCacheTokenProvider` also needs the `ITokenCache` of the
[shortest setup](index.md#code). Without the `Default` authority (registered by
`AddDefaultAuthority` or by the server authentication), every call throws
`NotSupportedException: The 'about' scheme is not supported.`

> [!CAUTION]
> Keep passwords and client secrets out of `appsettings.json` in source control: use user
> secrets, environment variables, a vault, or the encrypted values of the
> [Configuration](../configuration/index.md) guide.

## Send a remote secret

A remote secret is not a token: it is a value (typically an encrypted `user:password` pair) that
your service sends in a header, and that the receiving service turns into a token with its Basic
authentication middleware (see [Server authentication](../authentication-server/index.md)).
<xref:Arc4u.OAuth2.TokenProvider.RemoteClientSecretTokenProvider> calls no identity provider.

```json
{
  "Authentication": {
    "RemoteSecrets": {
      "Billing": {
        "ClientSecret": "<encrypted-secret>",
        "HeaderKey": "SecretKey"
      }
    }
  }
}
```

```csharp
builder.Services.AddRemoteSecretsAuthentication(builder.Configuration);
builder.Services.AddKeyedSingleton<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider.ProviderName);
```

`AddRemoteSecretsAuthentication` reads `Authentication:RemoteSecrets` (pass `sectionName` to use
another section). The configuration based `AddJwtAuthentication` already calls it.

| Key | Type | Default | Description |
|---|---|---|---|
| `Authentication:RemoteSecrets:<name>:ClientSecret` | `string` | none (required) | The value to send. |
| `Authentication:RemoteSecrets:<name>:HeaderKey` | `string` | `SecretKey` | The header that carries the value. `Basic` sends `Authorization: Basic <value>`: a clear `user:password` value is Base64 encoded first, a value already Base64 encoded is sent as is. |
| `Authentication:RemoteSecrets:<name>:AuthenticationType` | `string` | `Inject` | Keep `Inject`: the header name is taken from the token type only in that mode. |
| `Authentication:RemoteSecrets:<name>:ProviderId` | `string` | `RemoteSecret` | Key of the token provider. |

## Choose the token cache of an entry

By default every provider keeps its tokens in the cache of `Authentication:TokenCache`. An entry of
`Authentication:ClientTokens` or `Authentication:OnBehalfOf` can use another cache, or keep its
tokens for a shorter time, with its own `TokenCache`. To change all the entries of a section at once,
use its shared section: `Authentication:ClientTokensCache` or `Authentication:OnBehalfOfCache`.

```json
{
  "Authentication": {
    "TokenCache": { "CacheName": "Volatile", "MaxTime": "00:40:00" },
    "OnBehalfOfCache": { "CacheName": "Redis" },
    "OnBehalfOf": {
      "Inventory": { "ClientId": "<client-id>", "ClientSecret": "<client-secret>", "Scopes": [ "api://inventory/.default" ] },
      "Orders": {
        "ClientId": "<client-id>", "ClientSecret": "<client-secret>", "Scopes": [ "api://orders/.default" ],
        "TokenCache": { "MaxTime": "00:05:00" }
      }
    }
  }
}
```

Each value is taken from the entry, then from the shared section, then from
`Authentication:TokenCache`: here `Inventory` keeps its tokens in `Redis` for 40 minutes and
`Orders` in `Redis` for 5 minutes.

| Key | Type | Default | Description |
|---|---|---|---|
| `<entry>:TokenCache:CacheName` | `string` | the shared section, then `Authentication:TokenCache:CacheName` | The [named cache](../../concepts/glossary.md#named-cache) that keeps the tokens. An unknown name, or a cache that cannot be initialized when it is first used, uses the default cache until the application restarts. |
| `<entry>:TokenCache:MaxTime` | `TimeSpan` | the shared section, then `Authentication:TokenCache:MaxTime` | How long a token stays in the cache at most. It must be greater than zero. |
| `Authentication:ClientTokensCache` | object | none | `CacheName` and `MaxTime` shared by the `ClientTokens` entries. Read by `AddClientTokens` (parameter `tokenCacheSectionName`) and `AddJwtAuthentication` (`ClientTokensCacheSectionPath`); an empty path turns it off. |
| `Authentication:OnBehalfOfCache` | object | none | `CacheName` and `MaxTime` shared by the `OnBehalfOf` entries. Read by `AddOnBehalfOf` (parameter `tokenCacheSectionName`); an empty name turns it off. |

The options of an entry are registered as named `TokenCacheOptions` under the entry name (the
shared sections under `ClientTokensCache` and `OnBehalfOfCache`, so no entry can have these names
when the section exists), and the provider passes that name to the cache. A token is kept until it
expires, and at most `MaxTime`.

An entry registered from code with `AddOnBehalfOfSettings` sets `OnBehalfOfSettingsOptions.TokenCache`:
an empty `CacheName` takes the one of `Authentication:TokenCache`, `MaxTime` is the one of the
instance, and `Authentication:OnBehalfOfCache` does not apply. Your own code can register named options
with `AddNamedTokenCache` and pass their name as `optionsName` to the `ITokenCache`, and give its own
section of entries a shared section with `AddSharedTokenCache`:

```csharp
// "Authentication:ReportsCache" holds the CacheName and MaxTime shared by the entries of "Authentication:Reports".
var shared = services.AddSharedTokenCache(configuration, "Authentication:ReportsCache", "ReportsCache", reportEntryKeys);
foreach (var key in reportEntryKeys)
{
    services.AddNamedTokenCache(key, configuration.GetSection($"Authentication:Reports:{key}:TokenCache"), fallbackName: shared);
}
```

`AddSharedTokenCache` returns `null` when the section name is empty or the section does not exist, so
the entries then fall back on `Authentication:TokenCache`, and throws a `ConfigurationException` when
an entry has the name of the shared options.

A name under which no options are registered, for example a typo, uses the default options
(`Authentication:TokenCache`), and the warning event 9193 is logged once for that name. A name counts
as registered when `AddNamedTokenCache`, `AddSharedTokenCache`, `AddClientTokens`, `AddOnBehalfOf` or
`services.Configure<TokenCacheOptions>(name, …)` registers it. Any other registration, for example
`services.AddOptions<TokenCacheOptions>(name).BindConfiguration(path)`, counts only when its
`CacheName` or `MaxTime` differs from those of an unregistered name (the class defaults, as changed by
`ConfigureAll`): when the bound section is missing, the name uses the default options and the warning
is logged. The check is made once per name, at its first use, and kept until the application restarts.

At sign-out, the on-behalf-of tokens of the user are removed from the cache of their entry: the index
of the user stays in the cache of `Authentication:TokenCache` and records the cache of each token, so a
token is still removed when its options were changed or removed since it was cached.

## Cache keys

The providers cache the tokens they obtain. A cache key contains every value that changes the
token, and the secret parts (user token, password, client secret) are hashed with SHA-256 through
<xref:Arc4u.OAuth2.Token.TokenCacheKey>. The hash is the same in every process, so with a
distributed cache (Redis, SQL Server) the entries are shared between instances and survive a
restart:

| Provider | Key |
|---|---|
| `ClientCredentials` | `ClientCredentials_<client-id>_<hash>`, the hash covers the authority URL, the scope, the client secret and the extra parameters (for example `resource`, in any order). |
| `Credential` | `Credential_<user>_<hash>`, the hash covers the authority URL, the scope, the user and the password. |
| `Obo` | `_Obo_<client-id>_<hash>`, the hash covers the authority URL, the user's token and the scope, and the name of the cache when the setting has its own (see [Choose the token cache of an entry](#choose-the-token-cache-of-an-entry)). |

Two `ClientTokens` entries that differ only by an extra parameter such as `resource` get their
own cached token.

## Write your own token provider

Implement <xref:Arc4u.OAuth2.Token.ITokenProvider>, register it as a keyed service, and put its
key in the `ProviderId` of the settings. `GetTokenAsync` returns a `ValueTask<Result<TokenInfo>>`, so a
provider that answers from a cache or from the settings completes without allocating a `Task`. Return a
failed result rather than throwing when no token is available, the handlers then send the call
without a token.

```csharp
using Arc4u;
using Arc4u.OAuth2.Token;
using FluentResults;

public sealed class StaticTokenProvider : ITokenProvider
{
    public const string ProviderName = "Static";

    public ValueTask<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        if (settings is null || !settings.Values.TryGetValue("Token", out var token))
        {
            return ValueTask.FromResult(Result.Fail<TokenInfo>("No token configured."));
        }

        return ValueTask.FromResult(Result.Ok(new TokenInfo("Bearer", token, DateTime.UtcNow.AddMinutes(5))));
    }
}
```

```csharp
builder.Services.AddKeyedSingleton<ITokenProvider, StaticTokenProvider>(StaticTokenProvider.ProviderName);
```

When your provider keeps tokens for a user (in a cache, in memory, in a store), implement
<xref:Arc4u.OAuth2.Token.ISignOutTokenProvider> instead and remove them in `SignOutAsync`.
`SignOutUserAsync` of `AppPrincipalFactory` (client) and of `AppServicePrincipalFactory` (server)
signs the user out of every keyed provider that implements it (with
<xref:Arc4u.OAuth2.Token.SignOutTokenProviderExtensions.SignOutTokenProvidersAsync*>), once per
instance, and logs the exceptions so one provider cannot block the others. Every provider gets
the same settings, those given to `SignOutUserAsync`: ignore them when they are not yours, and do
not throw. A provider that only implements `ITokenProvider` (a machine-to-machine token, a secret,
the token of the incoming request) is skipped. Keep registering the provider as an `ITokenProvider`.

`AddOidcAuthentication`, `AddHybridAuthentication`, `AddOnBehalfOf`, `AddOidcClientAuthentication` and
`AddAuthenticationCookie` call `services.AddTokenProvidersSignOut()`: the providers are then resolved one
by one, so a provider that cannot be resolved (a missing dependency, a scoped provider resolved from a
singleton factory) is logged and the others are signed out. Call `AddTokenProvidersSignOut()` yourself
when your application uses none of these methods; otherwise all the keyed providers are resolved
together, and one that cannot be resolved prevents the sign-out of all of them.

Register `AppPrincipalFactory` as transient (or scoped), not as a singleton: it resolves the providers
from the scope it is created in, so a singleton would sign out root instances of the scoped services
instead of the ones the application uses.

| Provider | Sign-out |
|---|---|
| `AzureADOboTokenProvider` | Removes from their cache the on-behalf-of tokens of every API obtained for the current user, also with an access token that was refreshed since. Their keys are kept in an `OboTokenCacheIndex` per user (the registered `IUserObjectIdentifier`, as for the claims cache, otherwise the `oid` or `sub` claim) and application (`aud` claim, otherwise `azp` or `appid`), both read from the user token, so two applications sharing a cache do not remove each other's tokens: add this type to your `JsonSerializerContext` if the cache serializer uses one |
| `ClientTokenProvider` (Blazor) | Forgets the token kept in memory, and does not keep a token requested before the sign-out. Sign out of the backend cookie in your application |
| `OidcClientIdentityModelTokenProvider` | Cancels a login in progress, clears the tokens in memory and in the secure cache, then logs the user out of the authority (in the browser, bounded by `LoginTimeout`) when the user had a session. A refresh or a login in progress is not kept; concurrent calls refresh the token once |
| `UsernamePasswordTokenProvider` | Removes the user name, the password and the token from the secure cache. Does nothing with settings without `Authority` and `ServiceApplicationId` |
| `OidcTokenProvider` | None: the tokens are in the authentication cookie. Sign out of the cookie and OpenID Connect schemes with `HttpContext.SignOutAsync` |
| `CredentialTokenCacheTokenProvider` | None: the token is the one of a configured account, shared by all the users |
| `RemoteClientSecretTokenProvider`, `ClientCredentialsTokenProvider`, `BootstrapContextTokenProvider`, `NullTokenProvider` | None: they hold no user session |

To add a scenario to `Authentication:ClientTokens`, implement
<xref:Arc4u.OAuth2.TokenProvider.Scenarios.IClientTokenScenario> (its validation rules and the
settings it writes) and register it before `AddClientTokens` runs, keeping the built-in ones.
Here `MyScenario` is your implementation:

```csharp
ClientTokensExtension.SetScenarioResolver(discriminator => discriminator switch
{
    "MyScenario" => new MyScenario(),
    _ => ClientTokensExtension.DefaultScenario(discriminator)
});
```

## See also

- [HttpClient and gRPC clients](httpclient-grpc.md)
- [Server authentication](../authentication-server/index.md)
- [Caching](../caching/index.md), for the cache that keeps the tokens
- <xref:Arc4u.OAuth2.Token> in the API reference
