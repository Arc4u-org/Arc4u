# Documentation style guide

This guide defines how pages on the Arc4u documentation site
(https://arc4u-org.github.io/Arc4u/) are organized and written. Follow it together
with the page templates in [`docs/_templates/`](../_templates/). How to build and
preview the site is explained in [CONTRIBUTING.md](../../CONTRIBUTING.md#documentation).

This file and the templates are for contributors: they are excluded from the site
build (see `docfx.json`).

## Site structure

```text
docs/
  index.md                    landing page
  toc.yml                     top navigation: one entry per section
  getting-started/            install and first app (tutorials)
  concepts/                   architecture, principles, versioning, glossary
  guides/                     one folder per feature area
    <area>/index.md           the guide (full guide template)
    <area>/<topic>.md         optional sub-pages of that guide
    <area>/toc.yml            sidebar entries of that guide
    <area>/images/            images used by that guide
    package-support.md        package support matrix
  api/                        generated API reference (only index.md is written by hand)
  migration/                  migration guides between major versions
  releases/                   changelog
  _templates/                 page templates (not published)
  contributing/               this style guide (not published)
```

Rules:

- File and folder names are lowercase-kebab-case (`decrypting-secrets.md`), in English.
  The landing page of a folder is `index.md`.
- A guide folder contains flat Markdown files and at most one `images/` subfolder.
  Do not create other subfolders: the root `.gitignore` silently ignores folders named
  `bin`, `obj`, `debug`, `release`, `log` or `packages`.
- To add a sub-page to a guide, create `docs/guides/<area>/<topic>.md` and add it to
  `docs/guides/<area>/toc.yml` below `Overview`. Do not edit `docs/toc.yml`,
  `docs/guides/toc.yml` or another area's `toc.yml` for that.
- The `name` of a `toc.yml` entry is the page H1, or a shorter form of it.
- Package `README.md` files under `src/` are written from
  [`package-readme.md`](../_templates/package-readme.md), not from the site templates.
- Content from the legacy `Doc/` folder, the wiki or old package READMEs is rewritten into the
  page that owns its topic. Do not copy legacy files into `docs/` as they are.

## Page types

| Page | Template or outline |
|---|---|
| `getting-started/*.md` | [`tutorial.md`](../_templates/tutorial.md) |
| `concepts/*.md` except the glossary | [`concept.md`](../_templates/concept.md) |
| `concepts/glossary.md` | [Glossary outline](#glossary-outline) |
| `guides/<area>/index.md` and its sub-pages | [`guide.md`](../_templates/guide.md) |
| `guides/package-support.md` | [Package support matrix outline](#package-support-matrix-outline) |
| `migration/*.md` | [Migration guide outline](#migration-guide-outline) |
| `releases/index.md` and `CHANGELOG.md` | [Changelog outline](#changelog-outline) |
| `src/<Package>/README.md` | [`package-readme.md`](../_templates/package-readme.md) |

Each template starts with an HTML comment explaining its rules. Copy the template over
your stub, replace every `<...>` placeholder and delete the comment.

### Glossary outline

- One `##` heading per term, in alphabetical order. The heading is the term, as written in
  the docs (`## Facade`, `## Service agent`).
- One to three sentences of definition, then a link to the page that covers the term.
- The headings that already exist are [frozen anchors](#frozen-anchors). New terms can be added.
- Other pages link to a term with a path relative to their own folder: from a guide,
  `../../concepts/glossary.md#facade`; from another concept page, `glossary.md#facade`.

### Package support matrix outline

- One table per feature area, in the order of the guides, with the columns
  `Package | Status | Target frameworks | Guide`.
- `Status` is one of **Supported**, **Deprecated** or **Removed**. Target frameworks come from
  the package's `.csproj` on `develop/9.0.0`.
- A final section lists deprecated and removed packages with their replacement and a link to
  the migration guide.

### Migration guide outline

- Intro: who the page is for, and the versions it goes from and to.
- `## Before you start`: a short checklist (SDK, target framework, back up, read the changelog).
- One `##` per breaking change. Inside each: what changed and why (one or two sentences,
  link the issue), then a **Before (8.x)** code block, an **After (9)** code block and the
  steps to follow. The existing headings are [frozen anchors](#frozen-anchors); add a new
  `##` for a breaking change that is not listed yet.
- Renamed packages go in one table (`Old package | New package`) under `## Package renames`,
  not one section each.

### Changelog outline

- The single source of release notes is `CHANGELOG.md` at the repository root, in the
  [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/) format: `## [9.0.0] - YYYY-MM-DD`
  per version, then `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed`,
  `### Security`.
- `docs/releases/index.md` holds only its front matter and
  `[!INCLUDE [Changelog](../../CHANGELOG.md)]`. The H1 of `CHANGELOG.md` becomes the page title.
- Because `CHANGELOG.md` is rendered both on GitHub and inside the site, links from it to
  documentation pages must be absolute site URLs
  (`https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html`). Relative links to `docs/`
  break the site build.

## Front matter

Every page starts with YAML front matter holding a quoted one-sentence description, used in
search results and link previews. The page title is the H1, not a front matter field.

```markdown
---
description: "Configure named caches backed by memory, Redis, SQL Server or Dapr."
---
# Caching
```

## Voice and tone

- Write for a .NET developer who knows ASP.NET Core but not Arc4u.
- Address the reader as "you". Use the present tense and the active voice. Use the imperative
  in steps ("Add the package", not "You should add the package").
- Use American English spelling (behavior, initialize).
- Be direct and factual. Do not use marketing words, "simply", "just", "easy" or "obviously",
  and do not use emoji.
- One idea per sentence, one topic per paragraph. Prefer a list or a table to a long paragraph.
- Explain why before how when a choice is not obvious.
- Document what the code on `develop/9.0.0` does. Never describe an API, option, configuration
  key or default value you have not checked in the source. Describe planned work only as
  "not supported yet" with a link to the issue.
- Write product names correctly: Arc4u, .NET, ASP.NET Core, Blazor, Microsoft Entra ID,
  Azure AD B2C, ADFS, Keycloak, ForgeRock, OpenID Connect (OIDC), gRPC, Redis, Dapr,
  Serilog, OpenTelemetry, NuGet, Kubernetes.
- Put in `code` style: package names, namespaces, types, members, configuration keys,
  file names, paths, commands and literal values.

## Headings

- One H1 per page: the page title, same text as the `toc.yml` entry (or a longer form of it).
- Sentence case: capitalize only the first word and proper nouns (`## Common scenarios`).
- Do not skip levels (H2, then H3). Avoid H4.
- No trailing punctuation, no links, and no numbering in headings, except tutorial steps
  (`## Step 1: Create the project`).
- Keep headings short and unique within a page: they become anchors. DocFX builds the
  anchor from the heading in lowercase with hyphens (`## Common scenarios` becomes
  `#common-scenarios`); check the `id` in the built HTML if the heading contains punctuation.

## Code samples

### General rules

- Every C# sample must compile against `develop/9.0.0`, for `net10.0` and `net11.0`. If a
  sample only works on one of them, say so in a `> [!NOTE]`.
- Use the minimal hosting model: top-level statements, `WebApplication.CreateBuilder(args)`,
  `builder.Services`, `builder.Configuration`. No `Startup` class, no `IWebHostBuilder`.
- Nullable reference types and implicit usings are enabled (as in every Arc4u project).
  Write the `using` directives for Arc4u and third-party namespaces; omit the implicit ones.
- Use `async`/`await` end to end. No `.Result` or `.Wait()`.
- When the file matters, start the block with a comment naming it (`// Program.cs`).
- To elide code, write `// ...` between complete statements. The remaining code must still
  compile once the elided part is filled in the obvious way.
- Keep samples short: show the lines that matter, not a whole application.
- Never put real secrets, tenant IDs, host names or certificates in a sample. Use
  placeholders in angle brackets (`"<client-secret>"`) or `example.com`.
- Fence languages: `csharp`, `json`, `xml`, `bash` (for `dotnet` commands, which are the same
  on every OS), `powershell` (Windows-only commands), `yaml`, `http`, `text`, `mermaid`.
- Install commands do not pin versions. While Arc4u 9 is in preview, add `--prerelease`:

  ```bash
  dotnet add package Arc4u.Caching --prerelease
  ```

### JSON samples

- Valid JSON only: no comments, no trailing commas. Explain keys in the text or in a table.
- Show the full path from the root object, so it can be pasted into `appsettings.json`.
- Show only the keys the scenario needs, with values that work.

### Pull code from samples and tests when it exists

If the code already exists in `samples/` or in `src/Arc4u.UnitTest/`, include it with a DocFX
code snippet instead of copying it. The page then cannot drift from code that CI builds.
Mark the lines in the source file with a tag:

```csharp
// <register-cache>
builder.Services.AddCacheContext(builder.Configuration);
// </register-cache>
```

and reference the tag with a path relative to the Markdown file:

```markdown
[!code-csharp[](../../../samples/GettingStarted/Program.cs#register-cache)]
```

Use `// <tag>` markers rather than `#region` blocks: a `#region` snippet also copies any
nested `#region`/`#endregion` lines into the page.

A wrong path is a build warning (`codesnippet-not-found`), but a wrong **tag** is not: the
page silently shows an empty code block. Check every snippet in the local preview.

### Compile-check inline samples

Samples written inline in Markdown are compiled in a throwaway project outside the
repository that references the projects in `src/`. You need the SDK pinned in
`src/global.json` (see [CONTRIBUTING.md](../../CONTRIBUTING.md#prerequisites)).

```bash
cd "$(mktemp -d)"          # a fresh folder of your own, outside the repository
cp <repo>/src/global.json .
cat > DocSamples.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFrameworks>net10.0;net11.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
EOF
dotnet add reference <repo>/src/Arc4u.Caching/Arc4u.Caching.csproj   # one line per package the samples use
# paste one sample into Program.cs, then:
dotnet build
```

Only one file of a project can contain top-level statements, and most samples declare their
own `builder`. Check the samples one at a time in `Program.cs`, or keep one sample in
`Program.cs` and wrap each other sample in a method in its own file:

```csharp
// Sample2.cs
using Arc4u.Caching;

internal static class Sample2
{
    public static void Run(WebApplicationBuilder builder)
    {
        // paste the sample here, without its WebApplication.CreateBuilder line
        builder.Services.AddCacheContext(builder.Configuration);
    }
}
```

Use `Microsoft.NET.Sdk` instead of `Microsoft.NET.Sdk.Web` for samples that are not web
applications. The build must end with `0 Error(s)`; warnings coming from the `src/`
projects are not your concern. In the pull request, list which samples you compile-checked.

## Configuration and options

- Write configuration section paths exactly as the code reads them. `:` separates levels;
  a dot is part of a key name. `Authentication:OAuth2.Settings` is the key `OAuth2.Settings`
  inside the `Authentication` section, and `Application.Configuration` is a single top-level key.
- Take section names from the code (the default value of the `sectionName` parameter or the
  constant the method uses), and name the method and parameter that change it, for example
  "`AddCacheContext` reads the `Caching` section; pass `sectionName` to use another one."
- Document keys in a table with the columns `Key | Type | Default | Description`. The default
  is the value in the code; write "none (required)" when there is none. Do not document keys
  the code does not bind.
- Write keys with the casing of the options property they bind to (PascalCase).
- Refer to an options class by its type name, linked to the API reference on first mention:
  `<xref:Arc4u.OAuth2.Options.OAuth2SettingsOption>`.
- When environment variables matter (containers, Kubernetes), remind the reader that `__`
  replaces `:` in variable names.

## Diagrams

- Draw diagrams in Mermaid, in a `mermaid` code block. Do not commit images of diagrams and
  do not draw ASCII art.
- Use the simplest diagram type that works: `flowchart LR` or `flowchart TD` for components
  and layers, `sequenceDiagram` for request flows (authentication, token acquisition),
  `stateDiagram-v2` for state machines.
- Keep a diagram under about 15 nodes. Split it otherwise.
- Do not set colors or styles: the site switches between light and dark themes.
- Put labels with punctuation in quotes (`A["Arc4u.Caching (memory)"]`).
- Introduce every diagram with a sentence that says what it shows.
- The site renders Mermaid in the browser, so the build does not check the syntax. Check
  every diagram in the local preview (or on https://mermaid.live) before opening the PR.
- Mermaid does not render on NuGet.org: never put diagrams in package READMEs.

## Links

| Target | Syntax | Example |
|---|---|---|
| Another page | Relative path to the `.md` file | `[Caching](../caching/index.md)` |
| A section of a page | Path plus the heading anchor | `[Redis](../caching/index.md#redis)` |
| An Arc4u type or namespace | `xref` with the fully qualified name | `<xref:Arc4u.Caching.ICache>` |
| An Arc4u type, custom text | Link with an `xref:` target | `[the cache interface](xref:Arc4u.Caching.ICache)` |
| All overloads of a method | `xref` with `*` | `<xref:Arc4u.Caching.ICache.Get*>` |
| A .NET type | `xref`, resolved to learn.microsoft.com | `<xref:System.Text.Json.JsonSerializer>` |
| Source code without an API page | Absolute GitHub URL on `develop/9.0.0` | `https://github.com/Arc4u-org/Arc4u/blob/develop/9.0.0/src/...` |
| An issue or pull request | Absolute GitHub URL | `[#140](https://github.com/Arc4u-org/Arc4u/issues/140)` |

- Never link to a `.html` file, to an absolute URL of this site, or to the `master` branch
  from a page under `docs/`. DocFX rewrites `.md` links and checks them.
- An `xref` UID is the fully qualified name of the type or member (generic types use a
  backtick: ``<xref:Arc4u.Interval`1>``). After a build, `docs/_site/xrefmap.yml` lists every
  Arc4u UID. .NET UIDs are resolved through the Microsoft xref map configured in `docfx.json`.
- Use descriptive link text, never "here" or "this link".
- Prefer links to learn.microsoft.com without a locale (`https://learn.microsoft.com/aspnet/core/...`).
- Link issues only for context. The page must be understandable without reading them.
- The build reports broken page links (`InvalidFileLink`), broken anchors (`InvalidBookmark`)
  and unknown UIDs (`UidNotFound`) as warnings, and the documentation workflow treats
  warnings as errors.

### Frozen anchors

To link to a section of a page someone else is writing, use only the anchors below. Their
headings are frozen: page owners fill the sections and may reorder them, but never rename
them. Link to any other page owned by someone else without an anchor until that page is
merged.

| Page | Anchors |
|---|---|
| `migration/8x-to-9.md` | `#before-you-start`, `#package-renames`, `#target-frameworks`, `#newtonsoftjson-replaced-by-systemtextjson`, `#adal-and-protobuf-removed`, `#tracelisteners-removed`, `#msal-status`, `#nservicebus-replaced-by-dapr-pubsub`, `#prismdiwpf-replaced-by-prismdryioc`, `#message-and-messages-replaced-by-problemdetails`, `#icontainer-replaced-by-keyed-services`, `#authentication-settings-refactoring` |
| `concepts/glossary.md` | `#application-context`, `#appprincipal`, `#business-layer`, `#cache-context`, `#claims-filler`, `#data-access-layer`, `#domain-model`, `#facade`, `#interface-layer`, `#named-cache`, `#problemdetails`, `#result-pattern`, `#service-agent`, `#token-provider` |

For example, from `docs/guides/diagnostics/index.md`:
`[TraceListeners were removed](../../migration/8x-to-9.md#tracelisteners-removed)`.

## Admonitions

Use DocFX alerts, sparingly: at most one or two per section, never two in a row.

```markdown
> [!NOTE]
> Information worth knowing, even when skimming.
```

| Alert | Use it for |
|---|---|
| `> [!NOTE]` | Side information: a limit, a TFM difference, a default worth knowing. |
| `> [!TIP]` | A better or faster way to do something. |
| `> [!IMPORTANT]` | Something the reader must do for the feature to work. |
| `> [!WARNING]` | A mistake that breaks the application or the build. |
| `> [!CAUTION]` | A risk of losing data or weakening security (secrets, certificates, token validation). |

## Tabs

Use DocFX tabs only for true alternatives of the same step, for example controllers and
minimal APIs, or Windows and Linux. Use the same tab IDs across a page so that the reader's
choice is kept.

```markdown
# [Controllers](#tab/controllers)

...

# [Minimal APIs](#tab/minimal-apis)

...

---
```

## Images

- Prefer text and Mermaid. Use images only for screenshots (for example an identity provider's
  portal) that text cannot replace. Never use an image of code or configuration.
- Put the image in the `images/` folder next to the page that uses it
  (`docs/guides/authentication-server/images/entra-app-registration.png`), with a
  lowercase-kebab-case name.
- Use PNG for screenshots and SVG for drawings, and keep each file under 200 KB. Crop to the
  relevant part and hide personal or tenant data.
- Always write alt text that describes the content: `![Entra ID app registration, API permissions page](images/entra-app-registration.png)`.

## Package READMEs

Each package's `src/<Package>/README.md` is packed into the NuGet package and shown on NuGet.org.
Write it from [`package-readme.md`](../_templates/package-readme.md): a one-line purpose, the
install command, at most 10 lines of usage, and links to the guide and the API reference.
NuGet.org does not render Mermaid, DocFX alerts or relative links, so use plain Markdown and
absolute `https://` links only. The template lists the guide URL of each package.

## Before you open a pull request

- [ ] The page follows its template or outline, and the template comment is deleted.
- [ ] Every statement was checked against the code on `develop/9.0.0`.
- [ ] Every C# sample compiles (listed in the PR description).
- [ ] `dotnet docfx docfx.json` in `docs/` reports no warning from your files.
- [ ] You checked the page, its diagrams and its links in the local preview.
