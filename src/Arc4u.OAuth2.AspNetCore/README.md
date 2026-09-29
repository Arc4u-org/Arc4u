# Arc4u.OAuth2.AspNetCore

Creates the Arc4u principal of authenticated ASP.NET Core requests and provides the server-side authentication middlewares: forced OpenID Connect sign-in, Basic credentials, bearer injection and resource rights.

## Install

```bash
dotnet add package Arc4u.OAuth2.AspNetCore --prerelease
```

## Usage

```csharp
using Arc4u.OAuth2;
using Arc4u.OAuth2.Middleware;
using Microsoft.AspNetCore.Authentication;

builder.Services.AddScoped<IClaimsTransformation, AppPrincipalTransform>();
builder.Services.AddForceOpenId(builder.Configuration);
// ...
app.UseAuthentication();
app.UseForceOpenId();
```

`AppPrincipalTransform` turns the authenticated user into an `AppPrincipal`; `AddForceOpenId` reads `Authentication:ClaimsMiddleWare:ForceOpenId`.

## Documentation

- Guide: [Server authentication](https://arc4u-org.github.io/Arc4u/guides/authentication-server/)
- API reference: [Arc4u.OAuth2.AspNetCore](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.AspNetCore.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
