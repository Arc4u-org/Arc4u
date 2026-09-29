# Arc4u.OAuth2.AspNetCore.Authentication

Configures ASP.NET Core authentication from the `Authentication` configuration section: JWT bearer for APIs, OpenID Connect with a cookie session for web applications, or both.

## Install

```bash
dotnet add package Arc4u.OAuth2.AspNetCore.Authentication --prerelease
```

## Usage

```csharp
using Arc4u.OAuth2.Extensions;

builder.Services.AddJwtAuthentication(builder.Configuration);         // API: JWT bearer
// builder.Services.AddOidcAuthentication(builder.Configuration);    // web app: OpenID Connect + cookie
// builder.Services.AddHybridAuthentication(builder.Configuration);  // both
builder.Services.AddAuthorization();
```

Each method reads the `Authentication` section; the guide lists its keys and the Arc4u services (application context, caches, principal creation) your application registers.

## Documentation

- Guide: [Server authentication](https://arc4u-org.github.io/Arc4u/guides/authentication-server/)
- API reference: [Arc4u.OAuth2.Options](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.Options.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
