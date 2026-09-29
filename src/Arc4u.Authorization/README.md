# Arc4u.Authorization

Turns the operations of the Arc4u authorization model into ASP.NET Core authorization policies, per operation and per scope.

## Install

```bash
dotnet add package Arc4u.Authorization --prerelease
```

## Usage

```csharp
using Arc4u.Authorization;
using Arc4u.Security.Principal;

builder.Services.AddScopedOperationsPolicy(["Sales"], [new Operation { ID = 1, Name = "ReadOrders" }])
                .AddAnyOperations("OrdersAccess", 1);
// ...
app.MapGet("/orders", () => "orders").RequireAuthorization("ReadOrders");
```

This registers the policies `ReadOrders`, `Sales:ReadOrders` and `OrdersAccess`, evaluated against the principal of the Arc4u application context.

## Documentation

- Guide: [Claims and authorization](https://arc4u-org.github.io/Arc4u/guides/authentication-server/claims-and-authorization.html)
- API reference: [Arc4u.Authorization](https://arc4u-org.github.io/Arc4u/api/Arc4u.Authorization.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
