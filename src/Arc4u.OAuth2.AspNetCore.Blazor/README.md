# Arc4u.OAuth2.AspNetCore.Blazor

Sets the Arc4u `AppPrincipal` of the signed-in user in the `IApplicationContext` of Blazor Interactive Server components, whose SignalR circuit has its own dependency injection scope.

## Install

```bash
dotnet add package Arc4u.OAuth2.AspNetCore.Blazor --prerelease
```

## Usage

```csharp
builder.Services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
builder.Services.AddScoped<AuthenticationStateProvider, AppPrincipalServerAuthenticationStateProvider>();
builder.Services.AddScoped<CircuitHandler, ApplicationContextCircuitHandler>();
```

The principal is built from the claims of the authenticated user with the registered `IClaimAuthorizationFiller` and `IClaimProfileFiller`.

## Documentation

- Guide: [Blazor](https://arc4u-org.github.io/Arc4u/guides/authentication-client/blazor.html)
- API reference: [Arc4u.OAuth2.AspNetCore.Blazor](https://arc4u-org.github.io/Arc4u/api/Arc4u.OAuth2.AspNetCore.Blazor.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
