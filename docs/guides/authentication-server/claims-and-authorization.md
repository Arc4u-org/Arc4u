---
description: "Build the Arc4u principal from claims, load the user's rights with a claims filler, and protect endpoints with policies based on Arc4u operations."
---
# Claims and authorization

After authentication, Arc4u turns the ASP.NET Core `ClaimsPrincipal` into an
[AppPrincipal](../../concepts/glossary.md#appprincipal) that carries a user profile and an Arc4u
authorization: roles, operations and scopes. This page explains how the principal is built, how to add the
user's rights with a [claims filler](../../concepts/glossary.md#claims-filler), and how to check them with
ASP.NET Core authorization policies. It applies to the three scenarios of the
[overview](index.md).

## How the principal is built

<xref:Arc4u.OAuth2.AppPrincipalTransform> is an `IClaimsTransformation`: ASP.NET Core runs it after every
successful authentication. It:

1. When `LoadClaimsFromClaimsFillerProvider` is `true`, loads extra claims for the user: from the cache if
   present, otherwise from the registered <xref:Arc4u.Security.Principal.IClaimsFiller> (the result is then
   cached). Claims whose type is already on the identity are not added.
2. Builds the `Authorization` with <xref:Arc4u.Security.Principal.IClaimAuthorizationFiller> and the
   `UserProfile` with <xref:Arc4u.Security.Principal.IClaimProfileFiller>.
3. Creates the `AppPrincipal`, stores it in <xref:Arc4u.Security.Principal.IApplicationContext> and returns it,
   so `HttpContext.User` is the `AppPrincipal` too.

Inject `IApplicationContext` wherever you need the current user:

```csharp
using Arc4u.Security.Principal;

app.MapGet("/orders", (IApplicationContext context) =>
{
    var principal = context.Principal!;
    return principal.IsAuthorized(string.Empty, "ReadOrders")
        ? Results.Ok($"Orders of {principal.Profile.DisplayName}")
        : Results.Forbid();
}).RequireAuthorization();
```

## Claims filler options

`Authentication:ClaimsMiddleWare:ClaimsFiller` (<xref:Arc4u.OAuth2.Options.ClaimsFillerOptions>), read by the
configuration overloads (key `ClaimsFillerSectionPath`) or by `AddClaimsFiller(configuration)`:

| Key | Type | Default | Description |
|---|---|---|---|
| `LoadClaimsFromClaimsFillerProvider` | bool | `true` | Loads extra claims with the `IClaimsFiller`. When `true`, `IClaimsFiller`, `ICacheKeyGenerator` and `IUserObjectIdentifier` must be registered. |
| `ClaimsToExclude` | string array | `aud`, `iss`, `iat`, `nbf`, `acr`, `aio`, `appidacr`, `ipaddr`, `scp`, `tid`, `uti`, `unique_name`, `apptype`, `appid`, `ver` | Claim types returned by the claims filler that are not added. The claims already on the identity are never removed. The default list applies when the section or the key is missing. |
| `ExpireClaim` | string | `exp` | Claim type holding the expiration of the token. It cannot be part of `ClaimsToExclude`. |

The extra claims are cached in the cache named by `Authentication:TokenCache:CacheName`, for
`Authentication:TokenCache:MaxTime` (50 minutes by default), under the key
`<user identifier>_ClaimsCache`. The user identifier is the value of the first claim of the identity whose
type is listed in `Authentication:ClaimsIdentifier` (by default the object identifier claims
`http://schemas.microsoft.com/identity/claims/objectidentifier` and `oid`).

> [!IMPORTANT]
> When the token has none of the `ClaimsIdentifier` claim types, every authenticated request fails with a 500
> (`NullReferenceException: No distinguish key found for the identity`). Identity providers other than Microsoft
> Entra ID usually have no `oid` claim: set the identifier, for example `"ClaimsIdentifier": [ "sub" ]`.

## Load the user's rights with a claims filler

The default claims filler, <xref:Arc4u.OAuth2.Security.Principal.ClaimsBearerTokenExtractor>, returns the claims
of the user's access token (from the `BootstrapContext`, or from the token provider of the identity's
settings). With JWT bearer these claims are already on the identity; with a cookie session, it adds the claims
of the access token to those of the id token.

To give users Arc4u rights, implement `IClaimsFiller` and return the Arc4u authorization claim. The claim type
is `ClaimTypes.Authorization` of `Arc4u.IdentityModel.Claims`
(`http://schemas.arc4u.net/ws/2012/05/identity/claims/authorization`), and its value is the JSON of an
<xref:Arc4u.Security.Principal.Authorization>:

```csharp
// RightsClaimsFiller.cs
using System.Security.Principal;
using System.Text.Json;
using Arc4u.IdentityModel.Claims;
using Arc4u.Security.Principal;

public sealed class RightsClaimsFiller : IClaimsFiller
{
    public Task<IEnumerable<ClaimDto>> GetAsync(IIdentity identity)
    {
        // Read the rights of the user from your own store; hard-coded here.
        var authorization = new Authorization
        {
            AllOperations = [new Operation { ID = 1, Name = "ReadOrders" }, new Operation { ID = 2, Name = "WriteOrders" }],
            Operations = [new ScopedOperations { Scope = "", Operations = [1] }],
            Roles = [new ScopedRoles { Scope = "", Roles = ["Reader"] }],
        };

        IEnumerable<ClaimDto> claims =
        [
            new ClaimDto(ClaimTypes.Authorization, JsonSerializer.Serialize(authorization)),
        ];

        return Task.FromResult(claims);
    }
}
```

Register it instead of `ClaimsBearerTokenExtractor`:

```csharp
using Arc4u.Security.Principal;

builder.Services.AddTransient<IClaimsFiller, RightsClaimsFiller>();
```

The claims filler runs once per user and cache lifetime, inside the request that authenticates the user:
a slow store slows down that request only.

### The authorization model

| Member | Description |
|---|---|
| `AllOperations` | Every operation of the application: an `ID` (integer) and a `Name`. |
| `Operations` | Per scope, the identifiers of the operations granted to the user. |
| `Roles` | Per scope, the roles of the user. |
| `Scopes` | The scopes the user has access to. |

A scope is a free string that partitions the rights, for example a tenant or a business unit. The default scope
is the empty string. Each entry of `roles` and `operations` must have a `scope` (use `""` for the default
scope): an entry without scope makes the creation of the principal fail. The JSON of the example above is:

```json
{
  "roles": [ { "scope": "", "roles": [ "Reader" ] } ],
  "operations": [ { "scope": "", "operations": [ 1 ] } ],
  "allOperations": [ { "name": "ReadOrders", "id": 1 }, { "name": "WriteOrders", "id": 2 } ]
}
```

Only the operations listed in `AllOperations` are granted. In code, `AppPrincipal.IsAuthorized` accepts
operation identifiers or names, with or without a scope, and enum values (default scope);
`IsInRole(scope, role)` checks a role in a scope. To check a single operation name, pass the scope
(`IsAuthorized(string.Empty, "ReadOrders")`): `IsAuthorized("ReadOrders")` does not compile, because it matches
two overloads.

### Roles and IsInRole

`AppPrincipal.IsInRole(role)` checks the roles of the Arc4u authorization in the default scope, not the role
claims of the token. So `[Authorize(Roles = "Reader")]` and `RequireRole("Reader")` succeed only when the
authorization claim grants the role, even if the token has a `role` claim with that value.

## Protect endpoints with operation policies

`AddScopedOperationsPolicy` of `Arc4u.Authorization` registers one policy per operation, named as the operation,
and one per scope and operation, named `<scope>:<operation name>`:

```csharp
using Arc4u.Authorization;
using Arc4u.Security.Principal;

Operation[] operations =
[
    new Operation { ID = 1, Name = "ReadOrders" },
    new Operation { ID = 2, Name = "WriteOrders" },
];

builder.Services.AddScopedOperationsPolicy(["Sales"], operations)
                .AddAnyOperations("OrdersAccess", 1, 2)
                .AddAllOperations("OrdersAdmin", 1, 2);
```

```csharp
app.MapGet("/orders", () => "orders").RequireAuthorization("ReadOrders");
app.MapGet("/sales/orders", () => "orders").RequireAuthorization("Sales:ReadOrders");
```

With controllers, use `[Authorize(Policy = "ReadOrders")]`.

| Policy | Satisfied when |
|---|---|
| `ReadOrders` | Operation 1 is granted in the default scope. |
| `Sales:ReadOrders` | Operation 1 is granted in the `Sales` scope. An operation granted only in the default scope does not satisfy it. |
| `OrdersAccess` (`AddAnyOperations`) | At least one of the operations is granted. |
| `OrdersAdmin` (`AddAllOperations`) | All the operations are granted. |

<xref:Arc4u.Authorization.PoliciesBuilder> also has overloads with a scope or with `(scope, operation)` pairs,
`AddPolicy(name, configure)` for any other policy, and `ConfigureAuthorization` to change the
`AuthorizationOptions` (default policy, fallback policy). The handlers read the principal from
`IApplicationContext`: a request without principal fails every operation policy.

### Check a policy from code

Register <xref:Arc4u.Authorization.IApplicationAuthorizationPolicy> to evaluate a policy in a service that is
not an endpoint:

```csharp
using Arc4u.Authorization;

builder.Services.AddScoped<IApplicationAuthorizationPolicy, ApplicationAuthorizationPolicy>();
```

`IsAuthorizeAsync(policyName)` returns `false` when there is no principal or the policy is not satisfied;
`AuthorizeAsync(policyName, message)` throws `UnauthorizedAccessException` instead. In MVC controllers, the
`ManageExceptionsFilter` of `Arc4u.OAuth2.AspNetCore` (namespace `Arc4u.OAuth2.AspNetCore.Filters`) turns
this exception into a 403 `ProblemDetails`.

### Protect resource paths from configuration

`UseResourcesRightValidationFor` protects paths, typically an OpenAPI document, with a policy. When the
current principal does not satisfy the policy, the response is a 200 with a replacement content (by default an
empty Swagger 2.0 document titled "You are not authorized!"). Requests without principal pass through: combine
it with authentication on those paths.

```json
{
  "Authentication": {
    "ResourcesRights": {
      "ResourcesPolicies": {
        "OpenApi": {
          "Path": "/swagger/v1/swagger.json",
          "AuthorizationPolicy": "WriteOrders"
        }
      }
    }
  }
}
```

```csharp
using Arc4u.OAuth2.Middleware;

app.UseAuthentication();
app.UseResourcesRightValidationFor();
app.UseAuthorization();
```

| Key | Type | Default | Description |
|---|---|---|---|
| `ResourcesPolicies:<name>:Path` | string | none (required) | Request path, compared ignoring case and leading or trailing slashes. The entry name is only a label. |
| `ResourcesPolicies:<name>:AuthorizationPolicy` | string | none (required) | Policy the principal must satisfy. |
| `ResourcesPolicies:<name>:ContentToDisplay` | string | `DefaultContent` | Content returned when access is denied. |
| `DefaultContent` | string | an empty Swagger 2.0 document | Default content returned when access is denied. |

The method reads `Authentication:ResourcesRights`; pass `sectionName` to use another section.

> [!NOTE]
> Known issue: when the section does not exist, `UseResourcesRightValidationFor` adds nothing and does not
> report it. Check the path of the section if the resources are not protected.

## User profile

<xref:Arc4u.Security.Principal.ClaimsProfileFiller> fills the `UserProfile` from these claims. For the name,
the given name, the surname, the email and the `upn`, the long claim types of
`System.Security.Claims.ClaimTypes` are accepted too.

| Profile member | Claim |
|---|---|
| `DisplayName` | `name`; when absent, the account name derived from `upn`, or `given_name family_name` |
| `GivenName`, `SurName` | `given_name`, `family_name` |
| `Email` | `email` |
| `PrincipalName` | `upn` |
| `SamAccountName`, `Domain` | Derived from `upn` (`user@domain` or `DOMAIN\user`). The domain is mapped with `Authentication:DomainsMapping`, otherwise the first label of the domain is used. |
| `Name` | `<Domain>\<SamAccountName>` |
| `Sid` | The Arc4u `sid` claim, `S-1-0-0` when absent |
| `Company`, `Culture` | The Arc4u `company` and `culture` claims |

Replace `IClaimProfileFiller` to build the profile differently.

## Troubleshooting

### 403 with `[Authorize(Roles = ...)]` although the token has the role

See [Roles and IsInRole](#roles-and-isinrole): the roles come from the Arc4u authorization claim.

### 500 "No distinguish key found for the identity"

Set `Authentication:ClaimsIdentifier` to a claim type present in the tokens, see
[Claims filler options](#claims-filler-options).

### Every operation policy fails

The principal has an empty `Authorization`: the claims filler returned no authorization claim, or the claim
could not be deserialized (the error is logged). Check the claims of `HttpContext.User`, and that
`LoadClaimsFromClaimsFillerProvider` is `true`.

## See also

- [Server authentication](index.md)
- [AppPrincipal](../../concepts/glossary.md#appprincipal) and [claims filler](../../concepts/glossary.md#claims-filler) in the glossary
- <xref:Arc4u.Authorization.ScopedOperationsExtension>, <xref:Arc4u.OAuth2.Options.ClaimsFillerOptions> and <xref:Arc4u.Security.Principal.AppPrincipal> in the API reference
