# Arc4u.OAuth2

Options and services shared by the Arc4u authentication packages: identity provider authority, token cache, claims filler, user identifiers, token providers and custom root certificate authorities for `HttpClient`.

## Install

```bash
dotnet add package Arc4u.OAuth2 --prerelease
```

## Usage

```csharp
using Arc4u.OAuth2.Extensions;

builder.Services.AddDefaultAuthority(builder.Configuration);   // Authentication:DefaultAuthority
builder.Services.AddTokenCache(builder.Configuration);         // Authentication:TokenCache
builder.Services.AddClaimsFiller(builder.Configuration);       // Authentication:ClaimsMiddleWare:ClaimsFiller
```

In an ASP.NET Core application these registrations are done for you by `Arc4u.OAuth2.AspNetCore.Authentication`.

## Documentation

- Guide: [Server authentication](https://arc4u-org.github.io/Arc4u/guides/authentication-server/)
- API reference: [Arc4u.OAuth2](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
