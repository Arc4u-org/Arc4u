---
description: "Protect an ASP.NET Core API with JWT bearer tokens using AddJwtAuthentication, and optionally accept Basic credentials."
---
# JWT bearer

Use `AddJwtAuthentication` for an API called by other applications or by a front end that already holds an
access token. Each request carries `Authorization: Bearer <token>`; the token is validated by the ASP.NET Core
JWT bearer handler and turned into an [AppPrincipal](../../concepts/glossary.md#appprincipal). Start with
the [overview](index.md) for the services every scenario registers.

## Configuration

<xref:Arc4u.OAuth2.Extensions.AuthenticationExtensions.AddJwtAuthentication*> reads the `Authentication`
section (parameter `authenticationSectionName`) into <xref:Arc4u.OAuth2.Options.JwtAuthenticationSectionOptions>,
then the OAuth2 settings section into <xref:Arc4u.OAuth2.Options.OAuth2SettingsOption>.

### appsettings.json

```json
{
  "Caching": {
    "Default": "Volatile",
    "Caches": [
      {
        "Name": "Volatile",
        "Kind": "Memory",
        "IsAutoStart": true,
        "Settings": { "SizeLimitInMB": 10 }
      }
    ]
  },
  "Authentication": {
    "DefaultAuthority": { "Url": "https://login.example.com/my-tenant/v2.0" },
    "OAuth2.Settings": { "Audiences": [ "api://my-api" ] },
    "TokenCache": { "CacheName": "Volatile" }
  }
}
```

The `OAuth2.Settings` section (`Authentication:OAuth2.Settings`) describes the tokens the API accepts:

| Key | Type | Default | Description |
|---|---|---|---|
| `Audiences` | string array | empty | Accepted `aud` values. At least one is required while `ValidateAudience` is `true`, otherwise the registration throws `ConfigurationException: Audiences field is not defined.` |
| `ValidateAudience` | bool | `true` | Set to `false` to accept tokens without a matching audience (for example a Keycloak realm without an audience mapper). |
| `Authority` | object | none (the default authority) | Authority given to the JWT bearer handler as `Authority`. The metadata and signing keys are still read from `DefaultAuthority`. |
| `ProviderId` | string | `Bootstrap` | Key of the token provider that returns the token of the current request (see [Claims and authorization](claims-and-authorization.md)). |
| `Scopes` | string array | empty | Scopes stored with these settings, for token providers that exchange the token (on-behalf-of). |

The keys of the `Authentication` section itself:

| Key | Type | Default | Description |
|---|---|---|---|
| `DefaultAuthority` | object | none (required) | The identity provider, see [Configuration](index.md#configuration). |
| `OAuth2SettingsSectionPath` | string | `Authentication:OAuth2.Settings` | Section holding the OAuth2 settings above. |
| `CertSecurityKeyPath` | string | none | Section describing a certificate (`Store` or `File`, as for the [data protection certificate](oidc-cookie.md#data-protection-certificate)) whose key is used as issuer signing key. |
| `TokenCacheSectionPath` | string | `Authentication:TokenCache` | Token cache options. The `CacheName` is required. |
| `ClaimsIdentifierSectionPath` | string | `Authentication:ClaimsIdentifier` | Claim types that identify a user. |
| `ClaimsFillerSectionPath` | string | `Authentication:ClaimsMiddleWare:ClaimsFiller` | Options of the extra claims. |
| `DomainMappingsSectionPath` | string | `Authentication:DomainsMapping` | Domain mapping of the user profile. |
| `ClientTokensSectionPath` | string | `Authentication:ClientTokens` | Tokens for outgoing calls, see [Client authentication](../authentication-client/index.md). Optional. |
| `RemoteSecretSectionPath` | string | `Authentication:RemoteSecrets` | Secrets for outgoing calls, see [Client authentication](../authentication-client/index.md). Optional. |

`ValidateAuthority` and `ValidateAudience` are also bound on this section, but the JWT registration never reads
them (known issue): use `OAuth2.Settings:ValidateAudience` instead.

`Authentication:TokenCache:CacheName` is required even when the API makes no outgoing call: the cache also
holds the extra claims of the users.

### Code

```csharp
using Arc4u.OAuth2.Extensions;

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Register the Arc4u services listed in [Code](index.md#code) as well.

## What is validated

The handler is configured with:

- the metadata of `DefaultAuthority`, from which the signing keys are downloaded;
- `ValidAudiences` from `OAuth2.Settings:Audiences` when `ValidateAudience` is `true`;
- the token lifetime (ASP.NET Core default);
- `ValidateIssuer = false`: the `iss` claim is not compared. A token is accepted when it is signed by one of
  the keys of the authority's metadata.
- `MapInboundClaims = false`: claim types keep the names of the token (`oid`, `name`, `upn`, ...).

The token is kept in the `BootstrapContext` of the identity, whose authentication type is `OAuth2`.

> [!NOTE]
> Known issue: `AddJwtAuthentication` does not set the name claim type, and the claims are not mapped, so
> `HttpContext.User.Identity.Name` is `null` (the `AppPrincipal` profile is filled anyway). If your code reads
> `Identity.Name`, set the claim type after the registration:
>
> ```csharp
> using Microsoft.AspNetCore.Authentication.JwtBearer;
>
> builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
>     options => options.TokenValidationParameters.NameClaimType = "name");
> ```
>
> `AddHybridAuthentication` sets it from `NameClaimType` (default `name`).

> [!CAUTION]
> Because the issuer is not validated, the audience check is what ties a token to your API. Keep
> `ValidateAudience` enabled whenever your identity provider can issue an audience.

## Responses

<xref:Arc4u.OAuth2.Events.StandardBearerEvents> answers a failed or missing authentication with a 401 and a
JSON `ProblemDetails` body. For an expired token, the `x-token-expired` response header holds the expiration
date and the detail reads `The token expired on <date>`.

> [!NOTE]
> Known issue: the body of the 401 has `"status": 403`. Clients should use the HTTP status code. To change
> the response, derive from `StandardBearerEvents` as shown in
> [Extensibility points](index.md#extensibility-points).

A principal that is authenticated but does not satisfy a policy gets a 403 from ASP.NET Core.

## Common scenarios

### Configure the API from code

The `AddJwtAuthentication(IConfiguration, Action<JwtAuthenticationOptions>)` overload takes the options
directly. The `IConfiguration` parameter is not read by this overload.

```csharp
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;

builder.Services.AddTokenCache(options => options.CacheName = "Volatile");
builder.Services.AddClaimsFiller(options => options.LoadClaimsFromClaimsFillerProvider = false);
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
{
    options.DefaultAuthority = new AuthorityOptions(new Uri("https://login.example.com/my-tenant/v2.0"), null, null, null);
    options.OAuth2SettingsOptions = settings => settings.Audiences.Add("api://my-api");
    options.ClaimsIdentifierOptions = identifiers => identifiers.Add("oid");
});
```

Unlike the configuration overload, this overload does not register the token cache, the claims filler, the
domain mapping, the client tokens and the remote secrets: register those you need yourself, as above.

### Accept Basic credentials

Some clients (scripts, legacy tools) can only send a user name and a password. The Basic authentication
middleware exchanges them for an access token with the resource owner password grant of your identity
provider, and replaces the `Authorization` header with `Bearer <token>` before the JWT bearer handler runs.
The token is cached until one minute before it expires.

```json
{
  "Authentication": {
    "Basic": {
      "Settings": {
        "ClientId": "my-client-id",
        "Scopes": [ "api://my-api/access" ]
      },
      "DefaultUpn": "@contoso.com"
    }
  }
}
```

```csharp
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;

builder.Services.AddBasicAuthenticationSettings(builder.Configuration);
builder.Services.AddKeyedTransient<ITokenProvider, CredentialTokenCacheTokenProvider>(CredentialTokenCacheTokenProvider.ProviderName);
builder.Services.AddKeyedSingleton<ICredentialTokenProvider, CredentialTokenProvider>(CredentialTokenProvider.ProviderName);
builder.Services.AddSingleton<ITokenCache, ApplicationCache>();

// ...

app.UseBasicAuthentication();
app.UseAuthentication();
app.UseAuthorization();
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Authentication:Basic:Settings:ClientId` | string | none (required) | Client id used for the password grant. |
| `Authentication:Basic:Settings:Scopes` | string array | none (required) | Scopes requested. |
| `Authentication:Basic:Settings:ClientSecret` | string | none | Client secret, when the provider requires one for this grant. |
| `Authentication:Basic:Settings:Authority` | object | the default authority | Another authority for the password grant. |
| `Authentication:Basic:Settings:ProviderId` | string | `Credential` | Key of the token provider that requests the token. |
| `Authentication:Basic:DefaultUpn` | string | none | Suffix added to a user name that has no domain (`user@domain` or `DOMAIN\user`), for example `@contoso.com`. It must start with `@` followed by a domain name. |
| `Authentication:Basic:Certificates` | object | empty | Header (or query string parameter) names mapped to a certificate (`Store` or `File`) that decrypts a `user:password` pair sent in that header instead of a Basic header. |

`AddBasicAuthenticationSettings` throws `ConfigurationException` when the `Authentication:Basic` section is
missing. When the password grant fails, the request continues with its Basic header and the JWT bearer handler
answers 401.

> [!CAUTION]
> The password grant sends the user's password to your API. Many providers (Azure AD B2C, accounts with
> multi-factor authentication) do not support it. Use it only for clients that cannot do better, over HTTPS.

## Troubleshooting

### 401 with an invalid audience in the response detail

The `aud` claim of the token is not in `OAuth2.Settings:Audiences`. Decode the token and add its audience, or
request a token for the right scope.

### 401 with a signature or metadata error in the logs

The handler could not download the metadata or the token was not signed by the keys it lists. Check that
`DefaultAuthority:Url` (or `MetaDataAddress`) is reachable from the server and that the token comes from that
authority. For a private certificate authority, see [Custom root CA](custom-root-ca.md).

### 500 on every authenticated request

See [No user identifier found](troubleshooting.md#500-no-distinguish-key-found-for-the-identity).

## See also

- [Server authentication](index.md)
- [Hybrid](hybrid.md)
- [Claims and authorization](claims-and-authorization.md)
- <xref:Arc4u.OAuth2.Options.JwtAuthenticationSectionOptions> and <xref:Arc4u.OAuth2.Options.OAuth2SettingsOption> in the API reference
