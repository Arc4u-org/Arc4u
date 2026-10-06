---
description: "Configure a named cache stored in a Dapr state store with the Dapr kind."
---
# Dapr cache

The `Dapr` kind stores the values in a [Dapr state store](https://docs.dapr.io/developing-applications/building-blocks/state-management/).
Dapr talks to many backends (Redis, PostgreSQL, Azure Cosmos DB and others), so the choice of the store moves out of your
application and into a Dapr component file. This page extends the [caching guide](index.md).

## What it solves

`DaprCache` (<xref:Arc4u.Caching.Dapr.DaprCache>) is an `ICache` on top of the `DaprClient` of the `Dapr.Client` package.
It exposes a state store as a named Arc4u cache, next to caches of the other kinds.

Compared with the other kinds:

- Values are serialized by the Dapr client (System.Text.Json), not by an <xref:Arc4u.Serializer.IObjectSerialization>.
  There is no `SerializerName`, and you do not need to register a serializer for this kind.
- A sliding expiration is not supported: `Put` with `isSlided: true` throws `NotSupportedException`.
- Expiration is the `ttlInSeconds` metadata of the state store, so the state store must support time to live.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Caching.Dapr` | `DaprCache` (kind `Dapr`). References `Dapr.Client`. |
| `Arc4u.Caching` | `ICacheContext` and `AddCacheContext`. |

## Install

```bash
dotnet add package Arc4u.Caching.Dapr --prerelease
```

The application must run with a Dapr sidecar (`dapr run`, or the Dapr annotations on Kubernetes) and a state store component
whose `metadata.name` is the value you put in `Settings:Name`. The `DaprClient` finds the sidecar with the
`DAPR_HTTP_PORT` and `DAPR_GRPC_PORT` environment variables that the sidecar sets.

## Configuration

### appsettings.json

```json
{
  "Caching": {
    "Default": "Sidecar",
    "Caches": [
      {
        "Name": "Sidecar",
        "Kind": "Dapr",
        "IsAutoStart": true,
        "Settings": {
          "Name": "statestore"
        }
      }
    ]
  }
}
```

The settings bind to <xref:Arc4u.Configuration.Dapr.DaprCacheOption>:

| Key | Type | Default | Description |
|---|---|---|---|
| `Caching:Caches:n:Settings:Name` | `string` | empty | Name of the Dapr state store component, as in the `metadata.name` of its YAML file. Required in practice: an empty name makes every operation fail. |

The state store component is Dapr configuration, not Arc4u configuration. A minimal file for a local test, using the
in-memory state store of Dapr:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.in-memory
  version: v1
```

Use a persistent store, such as `state.redis`, in production. See the
[Dapr state store reference](https://docs.dapr.io/reference/components-reference/supported-state-stores/).

### Code

```csharp
// Program.cs
using Arc4u.Caching;
using Arc4u.Caching.Dapr;
using Arc4u.Dependency;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddKeyedTransient<ICache, DaprCache>(CacheContext.Dapr);

var app = builder.Build();
app.Run();
```

## Common scenarios

### Expire a value

```csharp
using Arc4u.Caching;

public static class DaprExpiration
{
    public static Task RunAsync(ICache cache)
    {
        // Absolute expiration only: the ttlInSeconds metadata is sent to the state store.
        return cache.PutAsync("report", TimeSpan.FromMinutes(5), "value");
    }
}
```

## Extensibility points

`DaprCache` is registered under the key `Dapr`. Register your own `ICache` under that key to replace it.

## Troubleshooting

### Operations fail to connect

No sidecar is reachable. Start the application with `dapr run`, or check the `DAPR_HTTP_PORT` and `DAPR_GRPC_PORT` variables.
The initialization creates the client but does not contact the sidecar, so the error appears on the first operation.

### `DataCacheException` on `Get` or `Put`

The Dapr client failed; its exception is the `InnerException`. The synchronous methods wait for the asynchronous Dapr calls,
so prefer `GetAsync` and `PutAsync` in a web application.

## See also

- [Caching](index.md)
- [Serialization](serialization.md)
- <xref:Arc4u.Caching.Dapr> in the API reference
