# GettingStarted sample

A minimal ASP.NET Core API that uses Arc4u for dependency injection (`[Export]` and the
`Arc4u.Dependency.Tool` source generator), configuration (`Application.Configuration`), logging
(`Arc4u.Diagnostics` on top of Serilog) and JWT bearer authentication (`AddJwtAuthentication`).

The walkthrough is on the documentation site:
https://arc4u-org.github.io/Arc4u/getting-started/first-app.html

## Run it

The sample references the Arc4u projects in `../../src`, so it builds against the current code.
You need:

- the .NET SDK pinned in [`src/global.json`](../../src/global.json) (the Arc4u projects also
  target .NET 11),
- the ASP.NET Core 10 runtime (the sample targets `net10.0`).

```bash
cd samples/GettingStarted
dotnet run
```

```bash
curl http://localhost:5080/hello/Ada   # 200: Hello Ada, from GettingStarted (Local).
curl -i http://localhost:5080/me       # 401, body {"status":403} (known issue: the body says 403)
```

No identity provider is needed to start the sample: requests without a token are rejected with
401. To call `/me` with a real token, set `Authentication:DefaultAuthority:Url` to your
identity provider's authority and `Authentication:OAuth2.Settings:Audiences` to the audience of
its access tokens, in `appsettings.json` or as environment variables:

```bash
Authentication__DefaultAuthority__Url=https://<your-idp>/realms/<realm> dotnet run
```

`AddJwtAuthentication` does not validate the `iss` claim of the token (known issue): any token
signed by one of the authority's keys is accepted, whatever its issuer, which matters with
providers whose tenants or realms share signing keys. `Program.cs` turns issuer validation back
on with `PostConfigure<JwtBearerOptions>(...)`, right after `AddJwtAuthentication`; keep that
line when you copy the sample.

The CI workflow [`.github/workflows/samples.yml`](../../.github/workflows/samples.yml) builds this
sample and checks both endpoints on every change to `src/` or `samples/`.
