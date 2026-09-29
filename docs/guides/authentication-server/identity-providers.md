---
description: "What to configure for Microsoft Entra ID, Azure AD B2C, ADFS, Keycloak and ForgeRock with the Arc4u server authentication."
---
# Identity providers

The Arc4u authentication uses standard OpenID Connect and OAuth2, so it works with any compliant provider. The
server authentication was tested by the maintainers with Microsoft Entra ID (formerly Azure AD), Azure AD B2C,
ADFS, Keycloak and ForgeRock when it replaced ADAL and MSAL in Arc4u 6.1. This page lists, per provider, what
the Arc4u code requires. Items marked **General guidance** come from the provider's documentation, not from the
Arc4u code or its issues: check them against your provider version. The claims discussed here end up in the
[AppPrincipal](../../concepts/glossary.md#appprincipal).

## Settings that depend on the provider

Whatever the provider, check these values first. They explain most sign-in failures.

| Setting | Why it depends on the provider |
|---|---|
| `DefaultAuthority:Url` | For OpenID Connect it must be equal to the `iss` claim of the access tokens (see [Token checks](oidc-cookie.md#token-checks)). |
| `DefaultAuthority:MetaDataAddress` | Needed when the metadata document is not at `<Url>/.well-known/openid-configuration`. |
| `OpenId.Settings:Scopes` | Arc4u adds no scope. Request `openid`, the scope that issues a refresh token, and a scope of your API so that the access token has its audience. |
| `OpenId.Settings:Audiences`, `OAuth2.Settings:Audiences` | The `aud` claim of the access tokens. Some providers issue none by default. |
| `ClaimsIdentifier` | The default (`oid`) only exists in Microsoft Entra ID tokens. |
| `ResponseType` | `code` works with Entra ID, Azure AD B2C and ADFS. For other providers, `code id_token token` may be needed (comment of `OidcAuthenticationSectionOptions.ResponseType`). |

## Microsoft Entra ID

- The defaults of Arc4u follow Entra ID: `oid` identifies the user, and `ClaimsToExclude` removes the
  Entra-specific claims (`aio`, `tid`, `uti`, `appidacr`, ...) returned by the claims filler.
- The on-behalf-of token provider (`Obo`) is written for Entra ID, see
  [Client authentication](../authentication-client/index.md).
- **General guidance**: with the v2.0 endpoint, set `Url` to
  `https://login.microsoftonline.com/<tenant-id>/v2.0`. The `iss` of access tokens issued for your API is that
  URL when the API application accepts v2 access tokens (`accessTokenAcceptedVersion: 2` in the manifest);
  v1 access tokens have the issuer `https://sts.windows.net/<tenant-id>/` and fail the Arc4u issuer check.
- **General guidance**: expose a scope on the API registration and request it
  (`api://<api-client-id>/<scope>`); add `offline_access` to get a refresh token. The audience of a v2 access
  token is the client id of the API.

```json
{
  "Authentication": {
    "DefaultAuthority": { "Url": "https://login.microsoftonline.com/<tenant-id>/v2.0" },
    "OpenId.Settings": {
      "ClientId": "<web-app-client-id>",
      "ClientSecret": "<client-secret>",
      "Audiences": [ "<api-client-id>" ],
      "Scopes": [ "openid", "profile", "offline_access", "api://<api-client-id>/access" ]
    }
  }
}
```

## Azure AD B2C

- Use the `code` response type (default).
- **General guidance**: the B2C metadata document depends on the user flow (policy), for example
  `https://<tenant>.b2clogin.com/<tenant>.onmicrosoft.com/<policy>/v2.0/.well-known/openid-configuration`. Set
  `DefaultAuthority:MetaDataAddress` to it, and `Url` to the issuer of the tokens given by that document.
- **General guidance**: B2C does not support the resource owner password grant for every account type; do not
  rely on [Basic credentials](jwt-bearer.md#accept-basic-credentials) with B2C.

## ADFS

- Use the `code` response type (default).
- Set `ClaimsIdentifier` to a claim of your ADFS tokens, for example `[ "upn" ]`. The default identifier, `oid`,
  is a Microsoft Entra ID claim (**general guidance**: ADFS does not issue it); without an identifier every
  request fails with a 500 (see [Troubleshooting](troubleshooting.md#500-no-distinguish-key-found-for-the-identity)).
- A `upn` claim of the form `user@domain` or `DOMAIN\user` fills `SamAccountName` and `Domain` of the
  profile. Map the domain with `Authentication:DomainsMapping`:

```json
{
  "Authentication": {
    "ClaimsIdentifier": [ "upn" ],
    "DomainsMapping": { "contoso.local": "CONTOSO" }
  }
}
```

- **General guidance**: the ADFS authority is `https://<adfs-host>/adfs`, whose metadata is at
  `https://<adfs-host>/adfs/.well-known/openid-configuration`.

## Keycloak

- Keycloak does not add your API to the audience of access tokens by default. Either add an audience mapper to
  the client in Keycloak (**general guidance**), or disable the audience check: `OAuth2.Settings:ValidateAudience: false`
  for JWT bearer, and the workaround of [Disable the audience check](oidc-cookie.md#disable-the-audience-check)
  for OpenID Connect.
- Set `ClaimsIdentifier` to a claim of your tokens, for example `[ "sub" ]` (**general guidance**: Keycloak does
  not issue the default `oid` claim).
- A development Keycloak over `http://` works: HTTPS is only required for the metadata when its address starts
  with `https://`. The redirect URI stays `http://` only for `localhost`
  (see [TLS termination](oidc-cookie.md#run-behind-a-tls-terminating-proxy)).
- **General guidance**: the authority is `https://<host>/realms/<realm>`; request `offline_access` for a refresh
  token.

```json
{
  "Authentication": {
    "DefaultAuthority": { "Url": "https://keycloak.example.com/realms/my-realm" },
    "ClaimsIdentifier": [ "sub" ],
    "OAuth2.Settings": { "ValidateAudience": false }
  }
}
```

## ForgeRock

The Arc4u code has nothing specific to ForgeRock. Apply the [provider-dependent settings](#settings-that-depend-on-the-provider):

- Set `ClaimsIdentifier` to a claim present in the tokens, for example `[ "sub" ]`.
- Check that the access tokens are JWTs: the cookie session reads the expiration from the access token and fails
  with an opaque token.
- **General guidance**: the authority is the realm URL of ForgeRock Access Management
  (`https://<host>/am/oauth2/realms/root/realms/<realm>`); make `Url` equal to the issuer given by its metadata.

## See also

- [OpenID Connect and cookie](oidc-cookie.md)
- [JWT bearer](jwt-bearer.md)
- [Troubleshooting](troubleshooting.md)
