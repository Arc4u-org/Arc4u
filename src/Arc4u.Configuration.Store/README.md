# Arc4u.Configuration.Store

A configuration provider that persists selected sections in a store and reloads them in a running application when they change. Use it with `Arc4u.Configuration.Store.EfCore` to keep the sections in a database.

## Install

```bash
dotnet add package Arc4u.Configuration.Store --prerelease
```

## Usage

```csharp
using Arc4u.Configuration.Store;

var builder = WebApplication.CreateBuilder(args);

// Persist a value. options.Add<T>("Section") persists a section read from the providers registered before.
builder.Configuration.AddSectionStoreConfiguration(options => options.Add("MaxItems", 42));
```

The store, the polling service and the startup call are described in the guide.

## Documentation

- Guide: [Configuration store](https://arc4u-org.github.io/Arc4u/guides/configuration/configuration-store.html)
- API reference: [Arc4u.Configuration.Store](https://arc4u-org.github.io/Arc4u/api/Arc4u.Configuration.Store.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
