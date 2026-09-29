---
description: "How Arc4u is versioned and how packages were renamed from Arc4u.Standard.* to Arc4u.*."
---
# Versioning and package naming

Arc4u follows semantic versioning since version 8.0.0, and every package of a release has
the same version number. With Arc4u 9, the packages were also renamed from
`Arc4u.Standard.*` to `Arc4u.*`. This page explains both, so that you can tell which
packages and versions belong together.

## How it works

### Version numbers

Since 8.0.0, a version is `MAJOR.MINOR.PATCH`:

| Part | Changes when |
|---|---|
| `MAJOR` | A release contains breaking changes. |
| `MINOR` | A release adds features, with bug fixes. |
| `PATCH` | A release contains bug fixes only. |

Previews carry a `-previewNN` suffix, for example `9.0.0-preview37`. Arc4u 9 is available only
as previews so far, which is why the install commands in this documentation use
`--prerelease`:

```bash
dotnet add package Arc4u.Caching --prerelease
```

All the packages are built and published together, with one version number passed to the
whole build. A package therefore gets a new version even when its own code did not change,
and the packages of one application should all use the same version.

The versions published on NuGet.org so far fall into these lines:

| Versions | Package names | Format | Target frameworks of the last release |
|---|---|---|---|
| 5.0.x.y (2021 to 2022) | `Arc4u.Standard.*` | Four parts, tied to .NET | `netstandard2.0`, `netstandard2.1` |
| 6.0.x.y and 6.1.x.y (2022 to 2023) | `Arc4u.Standard.*` | Four parts, tied to .NET | `netstandard2.0`, `net6.0`, `net7.0` |
| 8.0.0 to 8.3.2 (2023 to 2025) | `Arc4u.Standard.*` | Semantic versioning | `netstandard2.0`, `net8.0`, `net9.0` |
| 9.0.0 previews (since 2025) | `Arc4u.*` | Semantic versioning | `9.0.0-preview02` to `9.0.0-preview37`: `net8.0`, `net9.0`, `net10.0` (`9.0.0-preview01`: `net8.0`, `net9.0`) |

The code on the `develop/9.0.0` branch, which this site documents, targets `net10.0` and
`net11.0` (see [Target frameworks](../migration/8x-to-9.md#target-frameworks)).

> [!NOTE]
> A few packages also have a stable `1.0.0` version outside these lines, for example
> `Arc4u.Standard.Core`, `Arc4u.AspNetCore.Results` and `Arc4u.Dependency.Tool`. Ignore it:
> `dotnet add package Arc4u.Dependency.Tool` without `--prerelease` installs `1.0.0`, not a
> 9.0.0 preview.

Before 8.0.0, a version followed the .NET version it was built with. For example, `6.0.9.1`
was built with .NET 6.0.9:

- the first part was the .NET version supported,
- the second part changed for a breaking change in Arc4u,
- the third part was the minimum .NET patch version,
- the fourth part was the Arc4u change.

There is no Arc4u 7: the `Arc4u.Prism.DI.Wpf` package already had 7.2.x versions, so semantic
versioning started at 8.

### Package names

Arc4u 9 renamed the packages by removing `Standard` from their IDs:

| Arc4u 8.x | Arc4u 9 |
|---|---|
| `Arc4u.Standard` | `Arc4u` |
| `Arc4u.Standard.Core` | `Arc4u.Core` |
| `Arc4u.Standard.<Area>` | `Arc4u.<Area>`, for example `Arc4u.Standard.Caching.Redis` becomes `Arc4u.Caching.Redis` |

What the rename does and does not change:

- **The rename does not change namespaces.** The 8.x packages already used `Arc4u.*`
  namespaces. Other changes in Arc4u 9 moved or removed some types; the migration guide lists
  them.
- **Some packages already had the new name before 9**: `Arc4u.AspNetCore.Results`,
  `Arc4u.Configuration.Store.EfCore` and `Arc4u.Prism.DI.Wpf` (deprecated, not shipped in 9).
  `Arc4u.Configuration.Store` was published under its new name for `8.2.0-preview01` to
  `8.2.0-preview20`, then as `Arc4u.Standard.Configuration.Store` until 8.3.2.
- **A package ID can differ from its folder name** in the repository: `Arc4u.Caching.SqlServer`
  is built from `src/Arc4u.Caching.Sql`.
- **Paths of static web assets change.** ASP.NET Core serves the files of a Razor class library
  under `_content/<package ID>/`, so the Blazor files of `Arc4u.OAuth2.Blazor` are now under
  `_content/Arc4u.OAuth2.Blazor/`.
- **Some packages were not carried over** to Arc4u 9, and some are new. The
  [package support](../guides/package-support.md) page lists the status of each one.

> [!WARNING]
> Known issue: `BlazorController` in `Arc4u.OAuth2.AspNetCore.Blazor` still redirects to
> `_content/Arc4u.Standard.OAuth2.Blazor/GetToken.html`, which no longer exists after the rename,
> so that redirect returns 404.

The complete list of renamed packages is in
[Package renames](../migration/8x-to-9.md#package-renames) in the migration guide.

## Why Arc4u works this way

- **A version number should say what changed.** In the four-part scheme, the first and third
  parts described .NET rather than Arc4u, and because Arc4u supports several .NET versions at
  once they no longer said much. It was not possible to tell from a version whether it brought
  new features or only followed a .NET update. Semantic versioning answers that question.
- **One version for all packages** removes the need for a compatibility table: packages with the
  same version were built and tested together.
- **The `Standard` suffix no longer meant anything.** It dates from the packages that targeted
  .NET Standard only (the 5.x packages target only `netstandard2.0` and `netstandard2.1`).
  Arc4u 9 targets current .NET versions only, except `Arc4u.Dependency`, which also targets
  `netstandard2.0`, and the `Arc4u.Dependency.Tool` source generators, which target
  `netstandard2.0` as source generators must.

## Trade-offs

- **Previews are not stable.** An API can change between two `9.0.0-previewNN` versions.
  Read the [changelog](../releases/index.md) before you move to a newer preview.
- **New versions without changes.** Because all packages share a version, upgrading one means
  upgrading all, even those whose code is identical.
- **Two package families.** The `Arc4u.Standard.*` 8.x packages are still on NuGet.org next to
  the `Arc4u.*` 9 packages. They contain the same namespaces and type names, so do not
  reference both families in one application.

## In Arc4u

To keep every Arc4u package on the same version, use
[central package management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
and define the version once:

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <Arc4uVersion>9.0.0-preview37</Arc4uVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Arc4u.Caching" Version="$(Arc4uVersion)" />
    <PackageVersion Include="Arc4u.Diagnostics" Version="$(Arc4uVersion)" />
  </ItemGroup>
</Project>
```

To upgrade an application from the `Arc4u.Standard.*` 8.x packages, follow the
[migration guide](../migration/8x-to-9.md).

## See also

- [Package support](../guides/package-support.md)
- [Migrate from 8.x to 9](../migration/8x-to-9.md)
- [Releases](../releases/index.md)
- [Getting started](../getting-started/index.md)
