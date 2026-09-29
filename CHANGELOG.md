# Changelog

All notable changes to the Arc4u packages are documented in this file. The format is based on
[Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). Since 8.0.0, Arc4u follows
[semantic versioning](https://semver.org/spec/v2.0.0.html); earlier versions used a
`major.breaking.dotnet.minor` scheme tied to the .NET version (see
[Versioning](https://arc4u-org.github.io/Arc4u/concepts/versioning.html)).

Up to 8.3.2 the packages were named `Arc4u.Standard.*`; from 9.0.0 they are named `Arc4u.*`.
Dates are the dates the version was published on NuGet.

## [Unreleased]

Arc4u 9.0.0, developed on the `develop/9.0.0` branch and published on NuGet as `9.0.0-previewNN`
packages. It contains breaking changes: follow the
[migration guide from 8.x to 9](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html).

### Added

- `Arc4u.Dependency.Tool`: source generators that turn `[Export]` attributes and the
  `Application.Dependency` section into `IServiceCollection` registrations at compile time.
- `Arc4u.OAuth2.Client.Authentication`: OpenID Connect sign-in for desktop and mobile clients with
  `Duende.IdentityModel.OidcClient` (`AddOidcClientAuthentication`).
- `Arc4u.AspNetCore.Versioning`: API versioning helpers (`AddServiceApiVersioning`). Not published
  on NuGet yet.
- `AddHybridAuthentication`: OpenID Connect, cookies and JWT bearer in one registration.
  `AddOidcAuthentication` now registers OpenID Connect and cookies only.
- `AuthenticationMethod` (`RedirectGet` by default, or `FormPost`), `NameClaimType` and
  `RoleClaimType` settings for OpenID Connect.
- Client tokens: the `Authentication:ClientTokens` section with the `ClientCredentials`,
  `UserPassword` and `Basic` scenarios, and `ClientCredentialsTokenProvider`.
- Redis Sentinel cache (`RedisSentinelCache`, `AddRedisSentinelCache`, cache kind `RedisSentinel`),
  with password support.
- Custom root certificate authority support: `AddCustomRootCA`, and the `ConfigureLocalCaCertificate`
  and `ConfigureLocalCaCertificateForGrpc` extensions for `HttpClient` and gRPC clients.
- gRPC error mapping: `ToRpcException()` turns a failed `Result` into an `RpcException`, and
  `ToResult()` / `ToProblemDetailError()` read it back on the client.
- Authorization: `PoliciesBuilder` (returned by `AddScopedOperationsPolicy`) with `AddAllOperations`,
  `AddAnyOperations`, `AddPolicy` and `ConfigureAuthorization`, and the `AllScopedOperationsRequirement`
  and `AnyScopedOperationRequirement` requirements (all or any of the operations).
- Blazor: server-side authentication state (`AppPrincipalServerAuthenticationStateProvider`,
  `ApplicationContextCircuitHandler`), authentication state deserialization for the mixed
  server/WebAssembly mode, and `AddAuthenticationCookie`.
- Results: `ValidationError.Create`, `WithCode`, `WithSeverity`; implicit conversions from
  `ValidationError` and `ProblemDetailError` to `Result`; mappers with an asynchronous function;
  `ProblemDetails.WithInstance`.
- `Issuer` in `AuthorityOptions`.
- `NullLoggerWrapper` to test classes that use the Arc4u logging extensions.
- Ahead-of-time compilation support: the packages are marked `IsAotCompatible`, and
  `JsonSerialization` accepts a `JsonSerializerContext`.

### Changed

- Packages renamed from `Arc4u.Standard.*` to `Arc4u.*` (assembly names too). See
  [Package renames](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#package-renames).
- Target frameworks: `net10.0` and `net11.0` (the previews up to `9.0.0-preview37` target `net8.0`,
  `net9.0` and `net10.0`). See
  [Target frameworks](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#target-frameworks).
- Dependency injection uses the keyed services of `Microsoft.Extensions.DependencyInjection`
  instead of `IContainer`. See
  [IContainer replaced by keyed services](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#icontainer-replaced-by-keyed-services).
- Errors are returned as FluentResults `Result` values instead of `Messages` and `AppException`:
  `ITokenProvider.GetTokenAsync` returns `Result<TokenInfo>`, `IAppPrincipalFactory.CreatePrincipalAsync`
  returns `Result<AppPrincipal>`, `PersistEntity.TryValidate` returns `Result`. See
  [Message and Messages replaced by ProblemDetails](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#message-and-messages-replaced-by-problemdetails).
- Authentication settings: the OpenID Connect settings are registered under the name `Cookies`,
  the `AuthenticationType` and settings-key configuration entries are removed, events classes are
  registered in the service collection, and `IClaimsFiller.GetAsync` takes only the identity. See
  [Authentication settings refactoring](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#authentication-settings-refactoring).
- Logging: `Technical()`, `Business()` and `Monitoring()` return an `ILoggerWrapper<T>` and use the
  standard `Log…` methods; the logging is based on source-generated `LoggerMessage` methods
  ([#150](https://github.com/Arc4u-org/Arc4u/pull/150)). See
  [Fluent logging API](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#fluent-logging-api).
- `JwtHttpHandler` is generic (`JwtHttpHandler<T>`).
- `OnFailed` callbacks receive an `IReadOnlyCollection<IError>`; passing an asynchronous lambda to a
  synchronous `OnSuccess`, `OnFailed`, `OnSuccessNull` or `OnSuccessNotNull` is a compile error.
- `ValidateWithResult` moved to `Arc4u.FluentValidation` (namespace `Arc4u.Validation`).
- `UseForceOfOpenId(options)` is replaced by `AddForceOpenId(options)` and `UseForceOpenId()`.
- Uses System.Text.Json only. See
  [Newtonsoft.Json replaced by System.Text.Json](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#newtonsoftjson-replaced-by-systemtextjson).
- The authentication cookie expires after the shorter of `AuthenticationTicketTtl` and
  `RefreshTokenLifetime`, and its maximum age is `RefreshTokenLifetime`.
- Dapr packages updated to 1.18.

### Deprecated

- `Arc4u.Standard.NServiceBus`, `Arc4u.Standard.NServiceBus.Core`, `Arc4u.Standard.NServiceBus.RabbitMQ`
  and `Arc4u.Prism.DI.Wpf`: no 9.0 version
  ([#189](https://github.com/Arc4u-org/Arc4u/issues/189)). Use Dapr pub/sub and `Prism.DryIoc`.

### Removed

- .NET Standard 2.0, .NET 8 and .NET 9 targets.
- ADAL (`Arc4u.Standard.OAuth2.AspNetCore.Adal`), Protobuf serializers
  (`Arc4u.Standard.Serializer.Protobuf`, `Arc4u.Standard.Serializer.ProtobufV2`) and
  `Arc4u.Standard.Diagnostics.TraceListeners`.
- MSAL desktop support (`Arc4u.Standard.OAuth2.Msal`). See
  [MSAL status](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#msal-status).
- `IContainer`, `IContainerRegistry`, `IContainerResolve`, `ComponentModelContainer`,
  `DependencyContext` and the `Arc4u.Standard.Dependency.ComponentModel` package.
- `Message`, `Messages`, `MessageType`, `LocalizedMessage` and `AppException`.
- `Arc4u.Standard.OAuth2.AspNetCore.Api` (`ServiceAspectAttribute` for controllers).
- `Authentication:ClientSecrets` (`AddSecretAuthentication`, `CredentialSecretTokenProvider`), replaced
  by `Authentication:ClientTokens`.
- `LoggerContext` and the `Log()`-terminated logging API.
- The `DataContractSerializer`, `DataContractJsonSerializer` and `XmlSerializer` string helpers, and
  `Arc4u.Utils.Enum<T>` / `EnumUtil`.
- The AutoMapper dependency of `Arc4u.Diagnostics.Serilog.Sinks.RealmDb`.

### Fixed

- JWT bearer events missing in hybrid authentication.
- `TimeZoneContext` conversion from a specific local time.
- Anonymous calls through service discovery.
- Scope injection in `ILogger<T>`.

### Security

- Vulnerable dependencies upgraded ([#130](https://github.com/Arc4u-org/Arc4u/issues/130)).

## [8.3.2] - 2025-07-18

### Added

- `RefreshTokenLifetime` (default 90 days) in `OidcAuthenticationOptions` and
  `OidcAuthenticationSectionOptions`.
- Audience and authority validation in `TokenResponseReceived`, with explicit error messages.
- Configurable claims to exclude and expiration claim type in `ClaimsFillerOptions`;
  `AddClaimsFiller` adds `DefaultClaimsToExclude` and falls back to the OpenID settings.
- `LogMonitoringTimeElapsedMiddleware` supports minimal APIs.

### Changed

- Token expiration logic aligned between `StandardCookieEvents` and `RefreshTokenProvider`;
  `ITokenRefreshProvider`, `OidcTokenProvider` and `RefreshTokenProvider` refactored.
- `StandardBearerEvents` no longer returns a generic error description on challenge.
- `StandardOpenIdConnectEvents` uses `IOptionsMonitor` and reports clearer `OnRemoteFailure` errors.
- `OpenIdBearerInjectorMiddleware` sets `ClaimsIdentity.BootstrapContext`.
- gRPC: `AuthorizationInterceptor` no longer depends on `GrpcMethodInfo` and `ServiceAspectAttribute`;
  `OAuth2Interceptor` injects the token based on the `AuthenticationType` and the current identity.
- `Bound<T>`: better nullability, internal setters for `Type`, `Direction` and `Value`, `ToString()`
  shows `Bound.Infinity` for null values. `Interval<T>.GetHashCode()` simplified; `Period.ToString()`
  formatting updated.

### Fixed

- `BlazorMsalTokenProvider` no longer fails on a premature null check of the principal.

## [8.3.1] - 2025-05-05

### Fixed

- The OpenID Connect metadata address is derived from `DefaultAuthority` when it is not set,
  instead of being read before it was initialized.

## [8.3.0] - 2025-05-04

### Added

- `net9.0` target.
- `IAuditEntity<TAuditedBy, TAuditedOn>` interface ([#151](https://github.com/Arc4u-org/Arc4u/pull/151)).
- Retry mechanism in `CredentialTokenProvider` ([#146](https://github.com/Arc4u-org/Arc4u/pull/146)).
- Warning when a cache storage size limit is zero ([#148](https://github.com/Arc4u-org/Arc4u/pull/148)).

### Changed

- `BlazorController` updated for the new authentication model ([#147](https://github.com/Arc4u-org/Arc4u/pull/147)).
- Time measurements use `Stopwatch.GetTimestamp()` and `Stopwatch.GetElapsedTime()`
  ([#145](https://github.com/Arc4u-org/Arc4u/pull/145)).
- Validation errors are grouped by severity in `ValidationProblemDetails`.
- `DaprCache` and `MemoryCache`: time-to-live written with the invariant culture, nullable annotations
  and better error messages.
- Dependencies updated to .NET 9 (`System.Text.Json`, `Microsoft.Extensions.Hosting`,
  `Microsoft.AspNetCore.Authorization` 9.0.0).

### Removed

- `net6.0` target.
- `Arc4u.Standard.OAuth2.AspNetCore.Adal`, `Arc4u.Standard.Serializer.Protobuf`,
  `Arc4u.Standard.Serializer.ProtobufV2` and `Arc4u.Standard.Diagnostics.TraceListeners` are no
  longer published; their last version is 8.2.1.

## [8.2.1] - 2024-11-10

### Fixed

- OpenID Connect redirect URIs are forced to `https` (except `http://localhost`), for Kubernetes
  deployments where TLS ends at the ingress.

## [8.2.0] - 2024-08-16

### Added

- Extension methods for FluentResults in `Arc4u.Standard.Results`, and methods that return the matching
  HTTP result: Ok, NoContent, ProblemDetails, ValidationProblemDetails and Created (with or without a
  location).
- Encryption and decryption of long texts (more than 62 characters) by the configuration decryptor.
- `ValidateAudience` setting (default `true`) for the OpenID Connect and OAuth2 scenarios.
- A specific certificate for OpenID Connect and OAuth2 through an `IX509CertificateLoader` instance
  ([#109](https://github.com/Arc4u-org/Arc4u/pull/109)).
- Minimal API support.

### Changed

- The OpenID Connect registration no longer adds the `openid` and `offline_access` scopes: add them
  to the configuration yourself.
- `JwtHttpHandler` (`Arc4u.Standard.OAuth2.AspNetCore.Authentication`) no longer swallows exceptions.

### Deprecated

- The methods that inject `IScopedServiceProviderAccessor`: use `IServiceProvider`
  ([#108](https://github.com/Arc4u-org/Arc4u/pull/108)).

### Security

- Dependencies updated: `System.IdentityModel.Tokens.Jwt` (CVE-2024-21319, moderate),
  `Microsoft.Identity.Client` (CVE-2024-27086, low), `System.Text.Json` 8.0.4.

## [8.1.0] - 2024-02-07

### Added

- Extension methods in `Arc4u.Standard.Results` for FluentResults.
- A `Settings` section with a `Name` for the Dapr cache, to select the Dapr state store component.
- A default authentication scheme for services, so that a missing right returns 403.

### Changed

- `AddIf` logging extension takes a `Func<T>` for the value; the previous overload is obsolete.
- The activity ID moved from `AppPrincipal` to the `IApplicationContext` implementation.
- `RootCertificateExtractor` for gRPC refactored.
- Code uses the `IX509CertificateLoader` interface instead of `X509CertificateLoader`.
- Dependencies updated.

### Fixed

- The JSON decryptor configuration file supports several decryptors and works outside ASP.NET Core.

## [8.0.0] - 2023-12-15

### Added

- `net8.0` target (the packages target `netstandard2.0`, `net6.0`, `net7.0` and `net8.0`).

### Changed

- Version numbers follow semantic versioning.

## [6.1.18.1] - 2023-08-18

This version replaces ADAL and MSAL on the server side with the standard ASP.NET Core OpenID
Connect and JWT bearer handlers, so that any standard identity provider works (tested with Microsoft
Entra ID, Azure AD B2C, ADFS, Keycloak and ForgeRock). The settings are simplified and reorganized.

### Deprecated

- `Arc4u.Standard.OAuth2.AspNetCore.Adal`, `Arc4u.Standard.Serializer.Protobuf` and
  `Arc4u.Standard.Serializer.ProtobufV2`.

### Removed

- `Arc4u.Standard.OAuth.AspNetCore.Msal`, replaced by `Arc4u.Standard.OAuth2.AspNetCore.Authentication`.
- `Arc4u.Standard.KubeMQ` and `Arc4u.Standard.KubeMQ.AspNetCore`: use a message broker through
  [Dapr](https://dapr.io).

## [6.0.14.2] - 2023-03-08

### Added

- A custom `IX509CertificateLoader` can be injected in the configuration decryptor.

### Fixed

- The certificate loader of the configuration decryptor when no parameters are specified.

## [6.0.14.1] - 2023-02-17

### Added

- The configuration decryptor reads its certificate from PEM files (public and private key), for
  Kubernetes secrets.

## [6.0.13.1] - 2023-02-15

### Changed

- Dependencies updated to the matching .NET version.

## [6.0.12.1] - 2023-02-15

### Changed

- Dependencies updated to the matching .NET version.

## [6.0.11.2] - 2023-02-15

### Added

- `Arc4u.Standard.Configuration.Decryptor`: decrypts encrypted configuration values on the fly. See
  [Configuration](https://arc4u-org.github.io/Arc4u/guides/configuration/).
- Code analysis rules (`.editorconfig` of ASP.NET Core).

### Changed

- Configuration values are converted independently of the culture.
- Dependencies updated (.NET 6.0.11 and 7.0.1 kept).

### Fixed

- The Blazor client resolved its settings from `OAuth` instead of `OAuth2`.

## [6.0.11.1] - 2023-01-11

### Added

- `Arc4u.Standard.OAuth2.AspNetCore.Authentication`: OpenID Connect and OAuth2 authentication based on
  `Microsoft.AspNetCore.Authentication.OpenIdConnect` and `Microsoft.AspNetCore.Authentication.JwtBearer`,
  tested with Azure AD, Azure AD B2C and Keycloak.
- Support for .NET 6.0.11 and .NET 7.0.1.

### Changed

- `OAuthConfig` is removed. `ITokenUserCacheConfiguration` reads the claims that identify a user,
  `IUserObjectIdentifier` returns the unique identifier used for the token cache keys, and
  `KeyGeneratorFromIdentity` uses `IUserObjectIdentifier`. The ADAL and MSAL packages use these
  interfaces.

## [6.0.9.1] - 2022-09-17

### Changed

- Dependencies updated to .NET 6.0.9.

## [6.0.8.2] - 2022-09-16

### Security

- Fixed the user rights check of `ServiceAspect` (see 5.0.17.3).

## [5.0.17.3] - 2022-09-19

### Security

- Fixed the user rights check of `ServiceAspect`. Since .NET 5, attributes are created once and not
  per call, so an `IApplicationContext` injected in the attribute held the context of the first user.

## [5.0.10.3] - 2021-09-30

### Added

- FluentValidation rules `IsUtcDateTime` and `IsDate`.

## [5.0.10.2] - 2021-09-30

### Changed

- The FluentValidation rules `IsInsert`, `IsUpdate`, `IsDelete` and `IsNone` work on `IPersistEntity`.
- `IPersistEntity` and `PersistChange` moved from `Arc4u.Standard.Data` to `Arc4u.Standard`; the EF Core
  change tracker is based on `IPersistEntity`.

## [5.0.10.1] - 2021-09-28

### Added

- `Arc4u.Standard.FluentValidation` with the `IsInsert`, `IsUpdate`, `IsDelete` and `IsNone` rules.

### Changed

- The Blazor authentication pop-up window is relative to the base path of the application.

## [5.0.8.2] - 2021-08-19

### Fixed

- ADFS: the access token type returned by `CredentialTokenProvider`. `JwtHttpHandler` handles the
  `Inject` authentication type.

## [5.0.8.1] - 2021-08-11

### Added

- Blazor: extension methods to set the culture and to add policies based on operations.

### Changed

- Updated to .NET 5.0.8.
- `JwtHttpHandler` refactored; its constructor takes `IHttpContextAccessor` for the OAuth2 and
  OpenID Connect scenarios.
- EF Core change tracker based on `PersistEntity`.
- KubeMQ uses the serializers of the framework.

### Fixed

- gRPC `ClientErrorInterceptor` during server streaming.

## [5.0.6.1] - 2021-06-29

### Added

- `JsonSerialization` based on System.Text.Json.
- `OboTokenProvider` and `OboClientTokenProvider`.
- KubeMQ 2.2.8 features.

### Changed

- `JwtHttpHandler` refactored to work with `IHttpClientFactory`.

### Removed

- `ISerializationFactory`: inject the `IObjectSerialization` you need.

## [5.0.6] - 2021-05-21

### Changed

- Updated to ASP.NET Core 5.0.6.

## [5.0.5] - 2021-05-21

### Added

- First release based on ASP.NET Core 5.0.5.

[Unreleased]: https://www.nuget.org/packages/Arc4u
[8.3.2]: https://www.nuget.org/packages/Arc4u.Standard/8.3.2
[8.3.1]: https://www.nuget.org/packages/Arc4u.Standard/8.3.1
[8.3.0]: https://www.nuget.org/packages/Arc4u.Standard/8.3.0
[8.2.1]: https://www.nuget.org/packages/Arc4u.Standard/8.2.1
[8.2.0]: https://www.nuget.org/packages/Arc4u.Standard/8.2.0
[8.1.0]: https://www.nuget.org/packages/Arc4u.Standard/8.1.0
[8.0.0]: https://www.nuget.org/packages/Arc4u.Standard/8.0.0
[6.1.18.1]: https://www.nuget.org/packages/Arc4u.Standard/6.1.18.1
[6.0.14.2]: https://www.nuget.org/packages/Arc4u.Standard/6.0.14.2
[6.0.14.1]: https://www.nuget.org/packages/Arc4u.Standard/6.0.14.1
[6.0.13.1]: https://www.nuget.org/packages/Arc4u.Standard/6.0.13.1
[6.0.12.1]: https://www.nuget.org/packages/Arc4u.Standard/6.0.12.1
[6.0.11.2]: https://www.nuget.org/packages/Arc4u.Standard/6.0.11.2
[6.0.11.1]: https://www.nuget.org/packages/Arc4u.Standard/6.0.11.1
[6.0.9.1]: https://www.nuget.org/packages/Arc4u.Standard/6.0.9.1
[6.0.8.2]: https://www.nuget.org/packages/Arc4u.Standard/6.0.8.2
[5.0.17.3]: https://www.nuget.org/packages/Arc4u.Standard/5.0.17.3
[5.0.10.3]: https://www.nuget.org/packages/Arc4u.Standard/5.0.10.3
[5.0.10.2]: https://www.nuget.org/packages/Arc4u.Standard/5.0.10.2
[5.0.10.1]: https://www.nuget.org/packages/Arc4u.Standard/5.0.10.1
[5.0.8.2]: https://www.nuget.org/packages/Arc4u.Standard/5.0.8.2
[5.0.8.1]: https://www.nuget.org/packages/Arc4u.Standard/5.0.8.1
[5.0.6.1]: https://www.nuget.org/packages/Arc4u.Standard/5.0.6.1
[5.0.6]: https://www.nuget.org/packages/Arc4u.Standard/5.0.6
[5.0.5]: https://www.nuget.org/packages/Arc4u.Standard/5.0.5
