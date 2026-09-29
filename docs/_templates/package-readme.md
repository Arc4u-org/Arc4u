<!--
PACKAGE README TEMPLATE, for src/<Package>/README.md. This file is packed into the NuGet
package and is what NuGet.org shows on the package page. Replace every <...> and delete
this comment.

NuGet.org renders a limited Markdown: no Mermaid, no DocFX syntax (> [!NOTE], xref, tabs,
code snippets), no relative links or images (they resolve against nuget.org and break).
Use absolute https:// links only. Keep it short: the site holds the documentation.
The Usage block is at most 10 lines of C# and is compile-checked like any documentation sample.
Keep the four sections (title + one-line purpose, Install, Usage, Documentation) in this order.

Link to the guide of the package's area (trailing slash for the guide's index page,
.html for a sub-page, for example .../guides/caching/redis.html):

  Arc4u, Arc4u.Core, Arc4u.Threading                    https://arc4u-org.github.io/Arc4u/concepts/
  Arc4u.Dependency, Arc4u.Dependency.ComponentModel,
  Arc4u.Dependency.Tool                                 https://arc4u-org.github.io/Arc4u/guides/dependency-injection/
  Arc4u.Configuration*                                  https://arc4u-org.github.io/Arc4u/guides/configuration/
  Arc4u.Diagnostics*                                    https://arc4u-org.github.io/Arc4u/guides/diagnostics/
  Arc4u.OAuth2, Arc4u.OAuth2.AspNetCore,
  Arc4u.OAuth2.AspNetCore.Authentication,
  Arc4u.Authorization                                   https://arc4u-org.github.io/Arc4u/guides/authentication-server/
  Arc4u.OAuth2.Client, Arc4u.OAuth2.Client.Authentication,
  Arc4u.OAuth2.Blazor, Arc4u.OAuth2.AspNetCore.Blazor,
  Arc4u.OAuth.Msal                                      https://arc4u-org.github.io/Arc4u/guides/authentication-client/
  Arc4u.Caching*, Arc4u.Serializer*                     https://arc4u-org.github.io/Arc4u/guides/caching/
  Arc4u.Results, Arc4u.AspNetCore.Results,
  Arc4u.FluentValidation                                https://arc4u-org.github.io/Arc4u/guides/results/
  Arc4u.gRPC, Arc4u.AspNetCore.gRpc,
  Arc4u.AspNetCore.Versioning                           https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/
  Arc4u.Data, Arc4u.EfCore, Arc4u.MongoDB, Arc4u.OData  https://arc4u-org.github.io/Arc4u/guides/data/
  Arc4u.Dispatcher                                      https://arc4u-org.github.io/Arc4u/guides/dispatcher/

API reference: https://arc4u-org.github.io/Arc4u/api/<Namespace>.html (one page per namespace,
for example https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.html).
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
- API reference: [Arc4u.<Namespace>](https://arc4u-org.github.io/Arc4u/api/Arc4u.<Namespace>.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
