---
description: "Symptoms, causes and fixes for the Arc4u server authentication, built from past issues, plus the known issues of Arc4u 9."
---
# Troubleshooting

This page lists the errors met with the Arc4u server authentication, from the most frequent to the rarest,
with their cause and fix. Several come from past GitHub issues, linked for context. The last section lists the
known issues of `develop/9.0.0`. Start with the [overview](index.md) if the scenario itself is new to you, and
see [Concepts](../../concepts/index.md) for the vocabulary.

> [!TIP]
> Most errors are logged by the Arc4u events and middlewares with the `Technical` category. Enable the logs
> of the `Arc4u` and `Microsoft.AspNetCore.Authentication` categories at `Debug` level while you investigate
> (see [Diagnostics](../diagnostics/index.md)).

## Startup errors

The registration methods validate the configuration and throw when the application starts.

| Message | Cause and fix |
|---|---|
| `No section exists with name Authentication in the configuration providers ...` | The `Authentication` section is missing, or you use another name: pass it as `authenticationSectionName`. |
| `DefaultAuthority must be filled!` | Add `Authentication:DefaultAuthority:Url`. |
| `We need a cookie name defined specifically for your services.` | OpenID Connect and hybrid: add `Authentication:CookieName`. |
| `Audiences field is not defined.` | Add the audience of the tokens to `OAuth2.Settings:Audiences` (JWT bearer, hybrid) or `OpenId.Settings:Audiences` (OpenID Connect), or set `ValidateAudience` to `false` in that section. In hybrid mode, the `OAuth2.Settings` section is required. |
| `ClientId field is not defined.` or `Scopes field is not defined.` | Complete `Authentication:OpenId.Settings`. |
| `TokenCacheOptions.CacheName is not defined in the configuration file.` | Add `Authentication:TokenCache:CacheName`, the name of one of your caches. |
| `No certificate found for the given criteria.` | The data protection certificate of `Authentication:DataProtection:EncryptionCertificate:Store` is not in the store (or the process cannot read it). Check `Name`, `FindType`, `Location` and `StoreName`, or use the `File` form. |
| `Public key file doesn't exist.` or `Private key file doesn't exist.` | A path of the `File` form of a certificate is wrong. |
| `A section with name Authentication:DataProtection:CacheStore doesn't exist.` | Add `CacheKey` and `CacheName` for the data protection keys. |
| `The AuthenticationMethod should be either FormPost or RedirectGet.` | `Authentication:AuthenticationMethod` has another value. |
| `Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddAuthorization' ...` | Call `builder.Services.AddAuthorization()`: the Arc4u methods only register the authorization core services. |

The exception type depends on the method and the section (`ConfigurationException`, `MissingFieldException`,
`InvalidOperationException`, and `KeyNotFoundException` or `FileNotFoundException` for the certificates): search
for the message.

> [!NOTE]
> Arc4u 8.3.0 threw an `ArgumentNullException` on `MetaDataAddress` when the metadata address was not configured
> ([#153](https://github.com/Arc4u-org/Arc4u/issues/153)). Since 8.3.1 the address defaults to
> `<Url>/.well-known/openid-configuration`.

## API calls

### Every call returns 401

Check, in this order:

1. The `aud` claim of the token is in `OAuth2.Settings:Audiences`. When it is not, the response detail says that
   the audience is invalid (the explicit message was requested in [#35](https://github.com/Arc4u-org/Arc4u/issues/35)).
2. The token is not expired: the response has an `x-token-expired` header when it is.
3. The server can download `DefaultAuthority:MetaDataAddress`. Behind a private certificate authority, see
   [Custom root CA](custom-root-ca.md#trust-the-identity-provider).
4. The token is signed by the keys of that metadata: a token of another identity provider or environment fails.

### The 401 body says `"status": 403`

Known issue of `StandardBearerEvents`: the HTTP status is 401 but the `ProblemDetails.Status` is 403. Use the
HTTP status, or replace the events as shown in [Extensibility points](index.md#extensibility-points).

### 500: No distinguish key found for the identity

`NullReferenceException: No distinguish key found for the identity ...`: none of the `Authentication:ClaimsIdentifier`
claim types is in the token. The default claim types (`http://schemas.microsoft.com/identity/claims/objectidentifier`,
`oid`) are Microsoft identity platform claims; other providers usually identify the user with `sub`. Set it to a claim of
your tokens, for example `"ClaimsIdentifier": [ "sub" ]`. See [Identity providers](identity-providers.md).

### 500: Bad Arc4u usage

`InvalidOperationException: Bad Arc4u usage.` comes from the Arc4u logging extensions: `ILogger<T>` is not the
Arc4u logger. Call `builder.Services.AddApplicationContext()` (or `AddILogger()`, namespace `Arc4u.Dependency`, package `Arc4u.Diagnostics`), see
[Code](index.md#code).

### 403 with `[Authorize(Roles = ...)]` although the token has the role

`AppPrincipal.IsInRole` checks the roles of the Arc4u authorization, not the role claims of the token. See
[Roles and IsInRole](claims-and-authorization.md#roles-and-isinrole).

### Operation policies always fail

The principal has no authorization: implement a claims filler that returns the Arc4u authorization claim, see
[Load the user's rights](claims-and-authorization.md#load-the-users-rights-with-a-claims-filler).

### Caches are not initialized

The memory, Redis and SQL Server caches need an `IObjectSerialization` registration (`JsonSerialization` of
`Arc4u.Serializer.JSon`), and the data protection key store resolves it too. Without it, the cache logs that it
is not initialized. A missing registration used to fail silently in token handlers
([#81](https://github.com/Arc4u-org/Arc4u/issues/81)).

## Browser sign-in

### "There was an issue during the request: Invalid audience." or "... Invalid authority."

The access token returned at sign-in has an audience that is not in `OpenId.Settings:Audiences`, or an issuer
different from `DefaultAuthority:Url`. See [Token checks](oidc-cookie.md#token-checks).

### "You are not authenticated." with a 500

The sign-in threw an exception, logged by `StandardOpenIdConnectEvents`. With
`OpenId.Settings:ValidateAudience: false`, it is a known issue: see
[Disable the audience check](oidc-cookie.md#disable-the-audience-check).

### The identity provider rejects the redirect URI

Behind a proxy that ends TLS, the application sees `http`. Arc4u forces `https` in the redirect URI except for
`http://localhost` ([#131](https://github.com/Arc4u-org/Arc4u/issues/131)); forward the original host with the
forwarded headers middleware if the proxy changes it. See
[Run behind a TLS-terminating proxy](oidc-cookie.md#run-behind-a-tls-terminating-proxy).

### Users are signed out after the lifetime of the access token

No refresh token was issued: request `offline_access` (or your provider's equivalent) in
`OpenId.Settings:Scopes`. See [Session and refresh](oidc-cookie.md#session-and-refresh).

### Users lose their session when the application restarts or scales out

The data protection keys or the tickets are in a memory cache. Use a cache shared by the instances for
`Authentication:DataProtection:CacheStore` and the ticket store.

### Browser requests get a 401 in hybrid mode

The default challenge scheme is JWT bearer. See [Hybrid](hybrid.md#browser-requests-get-a-401-instead-of-the-sign-in-page).

### The access token is not in the BootstrapContext of a cookie session

It was requested in [#158](https://github.com/Arc4u-org/Arc4u/issues/158). Add `app.UseOpenIdBearerInjector()`
after `UseAuthentication()`, see [Use the user's access token](oidc-cookie.md#use-the-users-access-token).

### The first sign-in of a Blazor WebAssembly application fails

This is a client-side issue ([#155](https://github.com/Arc4u-org/Arc4u/issues/155)), see
[Client authentication](../authentication-client/index.md).

## Known issues

The following behaviors of `develop/9.0.0` are known. They are described where they matter, with a workaround
when there is one.

| Area | Issue | Workaround |
|---|---|---|
| OpenID Connect, hybrid | `ValidateAudience` and `ValidateAuthority` of `OidcAuthenticationOptions` are only applied by `AddOidcAuthentication(Action<OidcAuthenticationOptions>)`. The configuration overloads and both `AddHybridAuthentication` overloads keep them `true`. | `builder.Services.PostConfigure<OidcAuthenticationOptions>(o => o.ValidateAudience = false)` (or `ValidateAuthority`). See [Disable the audience check](oidc-cookie.md#disable-the-audience-check). |
| OpenID Connect, hybrid | With `OpenId.Settings:ValidateAudience: false` alone, the sign-in fails with `KeyNotFoundException` for the key `Audiences`. | Same `PostConfigure`. |
| OpenID Connect, hybrid | Security: the audience check of the access token is a substring search in the space-separated list of `OpenId.Settings:Audiences`, so an `aud` that is part of a configured audience (`api://my` for `api://my-api`) is accepted. | Use audiences that no other audience of your provider is a part of, or register the exact check of [Token checks](oidc-cookie.md#token-checks). |
| MVC filters | `ManageExceptionsFilter` throws `ReservedLoggingKeyException: ActivityId` in HTTP applications, so an `UnauthorizedAccessException` ends with a 500 instead of a 403. | Catch `UnauthorizedAccessException` yourself, see [Check a policy from code](claims-and-authorization.md#check-a-policy-from-code). |
| JWT bearer | The challenge answers 401 with a `ProblemDetails` whose `Status` is 403. | Replace `JwtBearerEvents`, see [Extensibility points](index.md#extensibility-points). |
| JWT bearer | The `iss` claim is not validated (`ValidateIssuer = false`): any token signed by the keys of the authority's metadata is accepted. | Keep the audience check enabled; add an issuer check in your own `JwtBearerEvents.TokenValidated` if you need one. |
| JWT bearer | `Authentication:ValidateAudience` and `Authentication:ValidateAuthority` are bound but never read by `AddJwtAuthentication`. | Use `Authentication:OAuth2.Settings:ValidateAudience`. |
| JWT bearer | `HttpContext.User.Identity.Name` is `null` with `AddJwtAuthentication`: no name claim type is set and the claims are not mapped. | `PostConfigure<JwtBearerOptions>` with `NameClaimType = "name"`, see [What is validated](jwt-bearer.md#what-is-validated). |
| All | `Authentication:TokenCache:CacheName` is required, even for an API that makes no outgoing call. | Point it to any configured cache. |
| Resource rights | `UseResourcesRightValidationFor(sectionName)` adds nothing, without error, when the section does not exist. | Check the section path. |
| Principal cache | `ServerPrincipalCache.Put(key, timeout, value, isSlided)` stores the `isSlided` boolean instead of the value, so the value is not cached. | Use the `PutAsync` overload, or the cache of `ICacheHelper.GetCache()`. |
| Custom root CA | When the standard certificate validation fails, `ConfigureLocalCaCertificate` (and `ConfigureLocalCaCertificateForGrpc`) only check the chain: a certificate of the custom authority for another host name is accepted. | Issue certificates of the private authority only to trusted services. |
| Middleware | `UseAddContextToPrincipal()` assumes that the request has an `Activity.Current` and answers 500 (`NullReferenceException`) when ASP.NET Core creates none, for example in an integration test without logging provider nor activity listener ([#124](https://github.com/Arc4u-org/Arc4u/issues/124)). | Keep a logging provider or an activity listener, or do not add the middleware in that host. |

## See also

- [Server authentication](index.md)
- [Identity providers](identity-providers.md)
- [Authentication settings in the 8.x to 9 migration guide](../../migration/8x-to-9.md#authentication-settings-refactoring)
