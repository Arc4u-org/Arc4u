# Arc4u.Configuration.Store.EfCore

Stores the sections of the Arc4u configuration store in a database through an EF Core `DbContext`.

## Install

```bash
dotnet add package Arc4u.Configuration.Store.EfCore --prerelease
```

## Usage

```csharp
using Arc4u.Configuration.Store;
using Arc4u.Dependency;
using Microsoft.EntityFrameworkCore;

builder.Configuration.AddSectionStoreConfiguration(options => options.Add("MaxItems", 42));
builder.Services.AddILogger();
builder.Services.AddDbContext<SettingsContext>(options => options.UseSqlite("Data Source=settings.db"));
builder.Services.AddDbContextSectionStore<SettingsContext>();
builder.Services.AddSectionStoreService();
```

`SettingsContext` maps the section entity with `modelBuilder.Entity<SectionEntity>().Configure()`. Call `app.Services.UseSectionStoreConfiguration()` after the database schema exists.

## Documentation

- Guide: [Configuration store](https://arc4u-org.github.io/Arc4u/guides/configuration/configuration-store.html)
- API reference: [Arc4u.Configuration.Store](https://arc4u-org.github.io/Arc4u/api/Arc4u.Configuration.Store.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
