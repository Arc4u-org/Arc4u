---
description: "<One sentence: what the reader can do after reading this guide. Shown in search results and link previews.>"
---
<!--
GUIDE TEMPLATE. Copy this file over your stub (docs/guides/<area>/index.md) and replace every <...>.

Rules (full details in docs/contributing/style-guide.md):
- Keep every H2 below, in this order, with exactly these titles. If a section has nothing
  to say, write one sentence saying so (for example "This package has no configuration.").
- The intro must link to the Concepts section (at least ../../concepts/index.md or a specific concept page).
- Every code sample must compile against develop/9.0.0 (net10.0 and net11.0); see the style guide.
- Link to API types with <xref:Namespace.Type>, to other pages with relative .md paths.
- Delete this comment before you open the PR.

Sub-pages (docs/guides/<area>/<topic>.md, listed in docs/guides/<area>/toc.yml):
- one sub-page per provider or per large topic (for example caching/redis.md);
- same front matter and intro rules; use the H2s of this template that apply
  (usually Configuration, Common scenarios, Troubleshooting, See also) and drop the others;
- the index.md of the guide keeps all sections and links to every sub-page.
-->
# <Area name, same as the toc.yml entry>

<Two to four sentences: what this feature area is and who needs it. Link the concept it builds on,
for example: Arc4u registers these services through [dependency injection](../../concepts/design-principles.md).>

## What it solves

<The problem, in the reader's terms, and what Arc4u adds on top of plain .NET. Say what Arc4u
deliberately leaves to .NET or to other libraries. Optional Mermaid diagram:>

```mermaid
flowchart LR
    App[Your application] --> Arc4u[Arc4u.<Package>] --> Backend[<Backend>]
```

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.<Package>` | <One line.> |
| `Arc4u.<Package>.<Provider>` | <One line.> |

<Say which package references which, so the reader installs the minimum.>

## Install

```bash
dotnet add package Arc4u.<Package> --prerelease
```

<Prerequisites outside NuGet, if any (a Redis server, a certificate, an identity provider).>

## Configuration

<What the section is called, which method reads it and the parameter that changes the section name.>

### appsettings.json

```json
{
  "<SectionName>": {
    "<Key>": "<value>"
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `<SectionName>:<Key>` | `<string>` | `<default from the code>` | <What it does.> |

### Code

```csharp
// Program.cs
using Arc4u.<Namespace>;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Add<Feature>(builder.Configuration);

var app = builder.Build();
app.Run();
```

## Common scenarios

### <Scenario written as a task, for example "Share a cache between two services">

<Short explanation, then the code or configuration. One H3 per scenario.>

## Extensibility points

<What the reader can replace through dependency injection, and how.>

| Service | Default implementation | Replace it to |
|---|---|---|
| <xref:Arc4u.<Namespace>.<IService>> | `<DefaultImplementation>` | <Reason to replace it.> |

```csharp
builder.Services.AddSingleton<IService, MyService>();
```

## Troubleshooting

### <Symptom as the reader sees it, for example the exception message or the HTTP status>

<Cause, then the fix. One H3 per symptom.>

## See also

- [<Related guide>](../<area>/index.md)
- [<Concept>](../../concepts/<page>.md)
- <xref:Arc4u.<Namespace>> in the API reference
