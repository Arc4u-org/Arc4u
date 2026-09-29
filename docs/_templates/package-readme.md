<!--
PACKAGE README TEMPLATE, for src/<Package>/README.md. This file is packed into the NuGet
package and is what NuGet.org shows on the package page. Replace every <...> and delete
this comment.

NuGet.org renders a limited Markdown: no Mermaid, no DocFX syntax (> [!NOTE], xref, tabs,
code snippets), no relative links or images (they resolve against nuget.org and break).
Use absolute https:// links only. Keep it short: the site holds the documentation.
The Usage block is at most 10 lines of C# and is compile-checked like any documentation sample.
Keep the four sections (title + one-line purpose, Install, Usage, Documentation) in this order.

Guide and API reference links per package (every package shipped in 9.x, that is every
packable project in src/Arc4u.slnx). Base URL: https://arc4u-org.github.io/Arc4u/
Use the guide URL as is for the guide's index page; for a sub-page use .../<topic>.html
(for example https://arc4u-org.github.io/Arc4u/guides/caching/redis.html). The API column is a namespace page that exists
in the API reference: package READMEs are not built by DocFX, so a wrong URL is never reported.
'(none)' means the package has no API page: omit the API reference line.
The package ID (title, install command) is the <PackageId> of the .csproj, not the folder name.

  Package                                  Guide                          API reference
  Arc4u                                    concepts/                      api/Arc4u.html
  Arc4u.Core                               concepts/                      api/Arc4u.Core.html
  Arc4u.Threading                          concepts/                      api/Arc4u.Threading.html
  Arc4u.Dependency                         guides/dependency-injection/   api/Arc4u.Dependency.html
  Arc4u.Dependency.Tool                    guides/dependency-injection/   (none)
  Arc4u.Configuration                      guides/configuration/          api/Arc4u.Configuration.html
  Arc4u.Configuration.Decryptor            guides/configuration/          api/Arc4u.Configuration.Decryptor.html
  Arc4u.Configuration.Store                guides/configuration/          api/Arc4u.Configuration.Store.html
  Arc4u.Configuration.Store.EfCore         guides/configuration/          api/Arc4u.Configuration.Store.html   (folder src/Arc4u.Configuration.Store.EFCore)
  Arc4u.Diagnostics                        guides/diagnostics/            api/Arc4u.Diagnostics.html
  Arc4u.Diagnostics.Serilog                guides/diagnostics/            api/Arc4u.Diagnostics.Serilog.html
  Arc4u.Diagnostics.Serilog.Sinks.RealmDb  guides/diagnostics/            api/Arc4u.Diagnostics.Serilog.Sinks.RealmDb.html
  Arc4u.OAuth2                             guides/authentication-server/  api/Arc4u.OAuth2.html
  Arc4u.OAuth2.AspNetCore                  guides/authentication-server/  api/Arc4u.OAuth2.AspNetCore.html
  Arc4u.OAuth2.AspNetCore.Authentication   guides/authentication-server/  api/Arc4u.OAuth2.Options.html
  Arc4u.Authorization                      guides/authentication-server/  api/Arc4u.Authorization.html
  Arc4u.OAuth2.Client                      guides/authentication-client/  api/Arc4u.OAuth2.Client.html
  Arc4u.OAuth2.Client.Authentication       guides/authentication-client/  api/Arc4u.OAuth2.Client.Authentication.html
  Arc4u.OAuth2.Blazor                      guides/authentication-client/  api/Arc4u.Blazor.html
  Arc4u.OAuth2.AspNetCore.Blazor           guides/authentication-client/  api/Arc4u.OAuth2.AspNetCore.Blazor.html
  Arc4u.Caching                            guides/caching/                api/Arc4u.Caching.html
  Arc4u.Caching.Memory                     guides/caching/                api/Arc4u.Caching.Memory.html
  Arc4u.Caching.Redis                      guides/caching/                api/Arc4u.Caching.Redis.html
  Arc4u.Caching.SqlServer                  guides/caching/                api/Arc4u.Caching.Sql.html   (folder src/Arc4u.Caching.Sql)
  Arc4u.Caching.Dapr                       guides/caching/                api/Arc4u.Caching.Dapr.html
  Arc4u.Serializer                         guides/caching/                api/Arc4u.Serializer.html
  Arc4u.Serializer.JSon                    guides/caching/                api/Arc4u.Serializer.html
  Arc4u.Results                            guides/results/                api/Arc4u.Results.html
  Arc4u.AspNetCore.Results                 guides/results/                api/Arc4u.AspNetCore.Results.html
  Arc4u.FluentValidation                   guides/results/                api/Arc4u.FluentValidation.html
  Arc4u.gRPC                               guides/grpc-versioning/        api/Arc4u.gRPC.html
  Arc4u.AspNetCore.gRpc                    guides/grpc-versioning/        api/Arc4u.AspNetCore.gRpc.html
  Arc4u.AspNetCore.Versioning              guides/grpc-versioning/        api/Arc4u.AspNetCore.Versioning.html   (not on NuGet.org yet)
  Arc4u.Data                               guides/data/                   api/Arc4u.Data.html
  Arc4u.EfCore                             guides/data/                   api/Arc4u.EfCore.html
  Arc4u.MongoDB                            guides/data/                   api/Arc4u.MongoDB.html
  Arc4u.OData                              guides/data/                   api/Arc4u.OData.html
  Arc4u.Dispatcher                         guides/dispatcher/             api/Arc4u.Dispatcher.Notification.html

Not shipped in 9.x (not in src/Arc4u.slnx, net8.0/net9.0 only): Arc4u.Dependency.ComponentModel,
Arc4u.OAuth2.Msal (project folder src/Arc4u.OAuth.Msal), and the removed NServiceBus and
Prism.DI.Wpf packages. Their status is on the Package support page (#189); do not write a
README from this template for them unless #189 decides they ship.
-->
# Arc4u.<Package>

<One sentence: what the package does and when to use it.>

## Install

```bash
dotnet add package Arc4u.<Package> --prerelease
```

## Usage

```csharp
builder.Services.Add<Feature>(builder.Configuration);
```

<One sentence on the configuration the snippet reads, if any.>

## Documentation

- Guide: [<Area name>](https://arc4u-org.github.io/Arc4u/guides/<area>/)
- API reference: [<Namespace>](https://arc4u-org.github.io/Arc4u/api/<Namespace>.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
