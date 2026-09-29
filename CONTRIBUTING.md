# Contributing to Arc4u

Thank you for helping improve Arc4u. This page explains how to build and test the
framework, how to propose a code change, and how to write and preview the documentation.

## Ways to contribute

- **Report a bug or request a feature** with the
  [issue templates](https://github.com/Arc4u-org/Arc4u/issues/new/choose). Include the Arc4u
  package versions, the target framework and a minimal reproduction.
- **Fix a bug or add a feature** with a pull request (see [Contribute code](#contribute-code)).
  For anything larger than a small fix, open an issue first so the approach can be agreed on.
- **Improve the documentation**: the site, the package READMEs or the XML comments
  (see [Documentation](#documentation)).

## Branches

The active development line is **`develop/9.0.0`** (Arc4u 9, `net10.0` and `net11.0`).
Branch from it and target it with your pull requests. Name your branch after the change,
for example `fix/redis-sentinel-timeout` or `docs/caching-guide`.

## Prerequisites

- The .NET SDK version pinned in [`src/global.json`](src/global.json). Roll-forward is
  disabled, so this exact version must be installed. You can install it next to your other
  SDKs with the [dotnet-install script](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script):

  ```bash
  ./dotnet-install.sh --jsonfile src/global.json
  ```

- The .NET 10 runtime, to run the tests for `net10.0` (the SDK above only brings the
  .NET 11 runtime).
- Git and any editor. Code style is defined in [`src/.editorconfig`](src/.editorconfig).

## Build

```bash
dotnet build src/Arc4u.slnx -c Release
```

Most packages target `net10.0` and `net11.0`. In `Release`,
the NuGet packages are generated as well.

## Test

The tests are in `src/Arc4u.UnitTest` (xUnit). CI runs the tests marked
`[Trait("Category", "CI")]`:

```bash
dotnet test src/Arc4u.UnitTest/Arc4u.UnitTest.csproj -c Release --filter "Category=CI"
```

Add `--framework net11.0` to run one target framework only, or drop the `--filter` option
to run every test.

## Contribute code

1. Fork the repository and create a branch from `develop/9.0.0`.
2. Make your change. Keep the pull request focused on one topic.
3. Add or update tests, and mark them with `[Trait("Category", "CI")]` so that CI runs them.
4. Document public types and members with XML comments (`<summary>`, `<param>`,
   `<returns>`). They generate the [API reference](https://arc4u-org.github.io/Arc4u/api/index.html).
5. If the change affects how Arc4u is used, update the matching guide under `docs/` and the
   package `README.md` (see below).
6. Build and run the tests, then open a pull request against `develop/9.0.0` that explains
   what changed and why, and links the issue it resolves.

## Documentation

The documentation site, https://arc4u-org.github.io/Arc4u/, is built with
[DocFX](https://dotnet.github.io/docfx/) from the Markdown files in [`docs/`](docs/) and from
the XML comments in `src/`. It is published from `develop/9.0.0`.

### Write a page

1. Find the page to write under `docs/`: the structure is described in the
   [style guide](docs/contributing/style-guide.md#site-structure). New sub-pages of a guide go
   in the guide's folder and in its `toc.yml`.
2. Start from the matching template in [`docs/_templates/`](docs/_templates/):
   - [`guide.md`](docs/_templates/guide.md) for a feature guide,
   - [`concept.md`](docs/_templates/concept.md) for a concept page,
   - [`tutorial.md`](docs/_templates/tutorial.md) for a Getting started page,
   - [`package-readme.md`](docs/_templates/package-readme.md) for a package `README.md`
     (the page NuGet.org shows).
3. Follow the [style guide](docs/contributing/style-guide.md): tone, headings, code samples
   (they must compile), configuration naming, Mermaid diagrams, links to the API reference,
   alerts and images.

### Preview the site

The API reference is generated from `src/`, so the SDK from
[Prerequisites](#prerequisites) is required. DocFX is pinned as a local tool.

```bash
cd docs
dotnet tool restore
dotnet docfx docfx.json --serve
```

The site is served on http://localhost:8080 (use `--port` to change it). The first build
compiles the projects in `src/` and takes a minute or two. DocFX does not watch for changes:
stop the server and run the command again. When you only changed Markdown, skip the API
generation to rebuild faster:

```bash
dotnet docfx build docfx.json --serve
```

### Check the build

CI builds the site with warnings treated as errors. Run the same check before you push:

```bash
cd docs
dotnet docfx docfx.json --warningsAsErrors
```

Broken links, unknown `xref` UIDs, missing code snippets and invalid XML comments are all
reported as warnings.

> [!NOTE]
> The documentation workflow and the XML comment fixes in `src/` are still being merged. Until
> then, the build also reports about 20 known warnings from `src/` files, and the site is not
> published automatically yet. Only warnings from files under `docs/` concern documentation
> changes.

## License

By contributing, you agree that your contributions are licensed under the
[license of this repository](LICENSE).
