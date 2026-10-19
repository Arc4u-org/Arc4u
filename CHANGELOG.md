# Changelog

All notable changes to the Arc4u packages are documented in this file. The format is based on
[Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). Since 8.0.0, Arc4u follows
[semantic versioning](https://semver.org/spec/v2.0.0.html); earlier versions used a
`major.breaking.dotnet.minor` scheme tied to the .NET version (see
[Versioning](https://arc4u-org.github.io/Arc4u/concepts/versioning.html)).

Up to 8.3.2 the packages were named `Arc4u.Standard.*`; from 9.0.0 they are named `Arc4u.*`.
Dates are the dates the version was published on NuGet. Published versions without an entry had
no release notes.

## [Unreleased]

Arc4u 9.0.0, developed on the `develop/9.0.0` branch and published on NuGet as `9.0.0-previewNN`
packages. It contains breaking changes: follow the
[migration guide from 8.x to 9](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html).

### Added

- `Arc4u.Diagnostics`: categories defined by the application. `MessageCategoryRegistry.Register("Security",
  (MessageCategory)8)` names a flag, and `logger.Category(category)` starts an entry in any category, so
  an application can write its own `logger.Security()` extension method. The name is written in the
  `Category` property and read back by `CategoryFilter`, `SimpleTextFormatter`, `MemoryLogDB` and `RealmDB`.
- `Arc4u.Dependency.Tool`: source generators that turn `[Export]` attributes and the
  `Application.Dependency` section into `IServiceCollection` registrations at compile time. Every
  `[Export]` attribute of a class or a record is registered, whatever its spelling
  ([#256](https://github.com/Arc4u-org/Arc4u/issues/256)); open generic exports are supported
  ([#257](https://github.com/Arc4u-org/Arc4u/issues/257)); `Version=` in a type name is matched against
  the referenced assembly, and `appsettings.json` may contain comments and trailing commas
  ([#258](https://github.com/Arc4u-org/Arc4u/issues/258)). Mistakes are reported as build diagnostics
  `ARC4UDEP001` to `ARC4UDEP007` ([#257](https://github.com/Arc4u-org/Arc4u/issues/257),
  [#258](https://github.com/Arc4u-org/Arc4u/issues/258)).
- `Arc4u.OAuth2.Client.Authentication`: OpenID Connect sign-in for desktop and mobile clients with
  `Duende.IdentityModel.OidcClient` (`AddOidcClientAuthentication`).
- `Arc4u.AspNetCore.Versioning`: API versioning helpers (`AddServiceApiVersioning`). Not published
  on NuGet yet.
- `AddHybridAuthentication`: OpenID Connect, cookies and JWT bearer in one registration.
  `AddOidcAuthentication` now registers OpenID Connect and cookies only.
- `AuthenticationMethod` (`RedirectGet` by default, or `FormPost`), `NameClaimType` and
  `RoleClaimType` settings for OpenID Connect. `NameClaimType` and `RoleClaimType` also apply to
  `AddJwtAuthentication`, so `User.Identity.Name` is set for a bearer token
  ([#224](https://github.com/Arc4u-org/Arc4u/issues/224)).
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
- Sign-out of the token providers: `ISignOutTokenProvider` for the providers that hold tokens for a
  user session, `SignOutTokenProvidersAsync` (signs out every registered one), `AddTokenProvidersSignOut()`
  and `SignOutTokenProviderKeys` (resolve each provider separately), `SignOutGeneration` (a token requested
  before a sign-out is not kept after it), `TechnicalLoggerExtensions.LogTechnicalException` (logs with or
  without an Arc4u logger) and `OboTokenCacheIndex`
  ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `LoginTimeout` and `SecureCacheTimeout` settings of the OpenID Connect sign-in of desktop and mobile
  clients (`OidcClientAuthenticationOptions`) ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `Arc4u.MongoDB`: `AddMongoDatabase<TContext>(configuration, connectionStringKey, options)` completes the
  `MongoClientSettings` read from the connection string with a delegate, for the settings a connection
  string cannot carry (`TranslationOptions` of the LINQ3 provider, `ServerApi`, `ClusterConfigurator`)
  ([#54](https://github.com/Arc4u-org/Arc4u/issues/54)).
- `AddResultLogger()`, which registers `FluentLogger` and gives it to FluentResults when the host
  starts, and `UseResultLogger()` to set it earlier
  ([#251](https://github.com/Arc4u-org/Arc4u/issues/251)).
- A `SetFromErrorFactory` overload whose function also receives the default translation, so a custom
  factory can fall back to it ([#249](https://github.com/Arc4u-org/Arc4u/issues/249)).
- `AddMemoryCacheKind()`, `AddRedisCacheKinds()`, `AddSqlCacheKind()` and `AddDaprCacheKind()`, which
  register the keyed `ICache` implementations that `ICacheContext` resolves by `Kind`; a `CacheContext`
  constructor that takes the section name, and `CacheContext.NormalizeKind`
  ([#242](https://github.com/Arc4u-org/Arc4u/issues/242)).
- `AddHttpContextLoggingProperties()` (`Arc4u.OAuth2.AspNetCore`): the loggers of singletons add the
  identity of the current HTTP request to their entries. Call it before `AddApplicationContext()` or
  `AddILogger()` ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- `IAuthorization.IsAuthorized(string operation)`, so that `IsAuthorized("Name")` compiles
  ([#225](https://github.com/Arc4u-org/Arc4u/issues/225)).
- `TryGetNamedSettings` (`Arc4u.OAuth2.Extensions.NamedSettingsExtensions`), an `IServiceProvider`
  extension that reads the named `SimpleKeyValueSettings` options, used by `AppPrincipalFactory` and the
  `JwtHttpHandler` and `OAuth2Interceptor` constructors that take a settings name. Such constructors are
  added to `JwtHttpHandler<T>` (`Arc4u.OAuth2.AspNetCore.Authentication`), the Blazor `JwtHttpHandler` and
  `OAuth2Interceptor<T>` ([#231](https://github.com/Arc4u-org/Arc4u/issues/231)).
- `SectionEntity(string key, string value)` constructor (`Arc4u.Configuration.Store`)
  ([#245](https://github.com/Arc4u-org/Arc4u/issues/245)).
- `MemoryCacheOption.SizeLimitInBytes` ([#241](https://github.com/Arc4u-org/Arc4u/issues/241)).
- `Arc4u.OAuth2.AspNetCore`: `ManageExceptionsHandler` (`AddManageExceptionsHandler` with
  `app.UseExceptionHandler()`) answers exceptions thrown outside MVC actions with the same problem details
  as `ManageExceptionsFilter`. `ManageExceptionsFilterOptions` adds the exception details to the response
  (`IncludeExceptionDetails`, by default in the `Development` environment only) and logs the action
  arguments (`LogActionArguments`, off by default, truncated to `MaxActionArgumentLength`) ([#31](https://github.com/Arc4u-org/Arc4u/issues/31)).

### Changed

- Packages are now licensed under `Apache-2.0` in their NuGet metadata, matching the `LICENSE`
  file; earlier versions declared `MIT`.
- Packages renamed from `Arc4u.Standard.*` to `Arc4u.*` (assembly names too). See
  [Package renames](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#package-renames).
- Target frameworks: `net10.0` and `net11.0` (the previews up to `9.0.0-preview37` target `net8.0`,
  `net9.0` and `net10.0`). See
  [Target frameworks](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#target-frameworks).
- Dependency injection uses the keyed services of `Microsoft.Extensions.DependencyInjection`
  instead of `IContainer`. See
  [IContainer replaced by keyed services](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#icontainer-replaced-by-keyed-services).
- Errors are returned as FluentResults `Result` values instead of `Messages` and `AppException`:
  `ITokenProvider.GetTokenAsync` and `ICredentialTokenProvider.GetTokenAsync` return
  `ValueTask<Result<TokenInfo>>`, and `IAppPrincipalFactory.CreatePrincipalAsync` returns
  `Result<AppPrincipal>`. See
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
- `OnFailed` and `OnFailedAsync` callbacks receive an `IReadOnlyCollection<IError>`; passing an
  asynchronous lambda to a synchronous `OnSuccess`, `OnFailed`, `OnSuccessNull` or `OnSuccessNotNull`
  is a compile error; the `…Async` methods on a plain `Result` or `Result<T>` return a `Task` instead of
  blocking with `.Wait()`.
- `ValidateWithResult` moved to `Arc4u.FluentValidation` (namespace `Arc4u.Validation`), and
  `ToFluentResultErrors` is renamed `ToResultErrors`.
- `UseForceOfOpenId(options)` is replaced by `AddForceOpenId(options)` and `UseForceOpenId()`.
- A custom ticket store or cookie post-configuration is registered in the service collection
  (`ITicketStore`, `IPostConfigureOptions<CookieAuthenticationOptions>`) instead of being named in
  configuration (`TicketStore`, `CookiesConfigureOptionsType`).
- `OidcAuthenticationOptions.AuthenticationTicketTTL` is renamed `AuthenticationTicketTtl`, and
  `Certificate` is renamed `DataProtectionCertificate`.
- The default `Authentication:TokenCache:MaxTime` is 50 minutes instead of 20.
- Uses System.Text.Json only. See
  [Newtonsoft.Json replaced by System.Text.Json](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#newtonsoftjson-replaced-by-systemtextjson).
- The authentication cookie expires after the shorter of `AuthenticationTicketTtl` and
  `RefreshTokenLifetime`, and its maximum age is `RefreshTokenLifetime`.
- Dapr packages updated to 1.18.
- `MemoryLogMessages` (`MemoryLogDbSink.LogMessages`) is a thread-safe `IReadOnlyCollection<LogMessage>`
  instead of a `List<LogMessage>`: use `Add`, `Clear`, `Count` and `GetPage`, and enumerate it to
  read a snapshot ([#238](https://github.com/Arc4u-org/Arc4u/issues/238)).
- `AddNotificationHandlersAsScoped` no longer takes a `ServiceLifetime`, which registered the
  services as scoped for any value but `Transient`. Call `AddNotificationHandlersAsScoped()` or the
  new `AddNotificationHandlersAsTransient()`
  ([#260](https://github.com/Arc4u-org/Arc4u/issues/260)).
- `IApplicationContext.Principal` is an `AppPrincipal?` and `SetPrincipal` accepts `null` to clear
  the principal, so the compiler warns when the principal is used without a null check
  ([#125](https://github.com/Arc4u-org/Arc4u/issues/125)).
- `ITokenProvider.SignOutAsync` moved to the new `ISignOutTokenProvider` interface, implemented only
  by the providers that hold tokens for a user session. `RemoteClientSecretTokenProvider`,
  `ClientCredentialsTokenProvider`, `BootstrapContextTokenProvider`, `NullTokenProvider`,
  `OidcTokenProvider` (its tokens are in the authentication cookie) and `CredentialTokenCacheTokenProvider`
  (a configured account, not a user session) no longer have a sign-out. `SignOutUserAsync` of
  `AppPrincipalFactory` and of `AppServicePrincipalFactory` signs the user out of every registered
  `ISignOutTokenProvider`, not only the provider of the `ProviderId`, and logs their exceptions instead of
  rethrowing them; `AppServicePrincipalFactory` only removed the cached claims before.
  `AddOidcAuthentication`, `AddHybridAuthentication`, `AddOnBehalfOf`, `AddOidcClientAuthentication` and
  `AddAuthenticationCookie` call `AddTokenProvidersSignOut()`, so a provider that cannot be resolved does
  not prevent the sign-out of the others. `AzureADOboTokenProvider` keeps an `OboTokenCacheIndex` in the
  cache: add it to your `JsonSerializerContext` if the cache serializer uses one. See
  [Other API changes](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#other-api-changes)
  ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `BlazorMsalTokenProvider` takes the public `IAccessTokenProvider` instead of
  `IAccessTokenProviderAccessor`, an internal API of `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
  that can change in any release. Its registration does not change; code that creates it must pass an
  `IAccessTokenProvider` ([#83](https://github.com/Arc4u-org/Arc4u/issues/83)).
- `AddJwtAuthentication` and the bearer scheme of `AddHybridAuthentication` validate the token issuer
  (`Authentication:ValidateAuthority`, `true` by default), and the OpenID Connect sign-in applies the
  same rule: the issuer must be `Authentication:DefaultAuthority:Issuer` when it is set, or the issuer of the provider's
  metadata. The configuration overloads of `AddOidcAuthentication` (as in 8.x) and
  `AddHybridAuthentication` apply `ValidateAudience` and `ValidateAuthority` from the `Authentication` section
  ([#220](https://github.com/Arc4u-org/Arc4u/issues/220)).
- The `Authentication:TokenCache` section is optional for `AddJwtAuthentication`, for an API that only
  validates inbound tokens ([#224](https://github.com/Arc4u-org/Arc4u/issues/224)).
- The `Identity` log property is the email of the user profile, or the identity name when there is no
  email. A logger resolved in a scope uses the `IAddPropertiesToLog` registered under the key `Scoped`,
  and a logger resolved from the root (for a singleton) the one under the key `Transient`
  ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- The client `JwtHttpHandler` classes and `OAuth2Interceptor` send the request without a token instead
  of throwing when the settings have no `ProviderId`, the provider is not registered, the
  provider returns no token (`NullTokenProvider`) or, for the interceptor, there is no current HTTP
  request. `OAuth2Interceptor` no longer blocks on `.Result` for asynchronous unary calls.
  `OidcClientIdentityModelTokenProvider` returns a failed result when the login fails, and logs the user
  in again when the refresh fails, instead of throwing ([#230](https://github.com/Arc4u-org/Arc4u/issues/230)).
- `ICache`: `Put` saves the default value of a value type (`0`, `false`), `Get` and `Put` of every cache
  wrap an engine or serialization error in a `DataCacheException` that keeps the original exception as its
  inner exception, the Dapr cache also rejects a `null` value and its `TryGetValue` returns `false` for a
  missing key, and a memory cache that is full throws a
  `DataCacheException` instead of dropping the value ([#243](https://github.com/Arc4u-org/Arc4u/issues/243)).
- `ToActionCreatedResultAsync`, `ToHttpCreatedResultAsync` and `ToTypedCreatedResultAsync` on a `Result`
  without a value lost their unused type parameter, and the title of the generic `500` problem details is
  "A technical error occurred!" (the typo "occured" is fixed) ([#252](https://github.com/Arc4u-org/Arc4u/issues/252)).
- An authorization `Operations` or `Roles` entry without a `scope` throws a `ConfigurationException` when
  the principal is created; use the empty string for the default scope
  ([#226](https://github.com/Arc4u-org/Arc4u/issues/226)).
- `AddApplicationConfig(configuration)` throws a `ConfigurationException` instead of a
  `NullReferenceException` when the section is missing or empty
  ([#248](https://github.com/Arc4u-org/Arc4u/issues/248)).
- `Arc4u.Configuration.Store.EfCore`: the static class `ServiceCollectionExtensions` is renamed
  `DbContextSectionStoreServiceCollectionExtensions` ([#245](https://github.com/Arc4u-org/Arc4u/issues/245)).
- `ManageExceptionsFilter` logs the request context (HTTP method, path, action and route values) with
  the exception. In the `Development` environment its responses now carry the exception details unless
  `ManageExceptionsFilterOptions.IncludeExceptionDetails` is set to `false` ([#31](https://github.com/Arc4u-org/Arc4u/issues/31)).
- `AddApplicationContext()` no longer registers `DefaultLoggingProperties`: register it with
  `AddKeyedScoped<IAddPropertiesToLog, DefaultLoggingProperties>("Scoped")` to add the `Identity` property
  to the log entries ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- Compared with the previews, the `Arc4u.Dependency.Tool` package contains the generator as an analyzer only: no assembly is
  copied to the output of the application ([#259](https://github.com/Arc4u-org/Arc4u/issues/259)).

### Removed

- `IApplicationContext.ActivityID`: the `ActivityId` log property is the trace id of the current
  `System.Diagnostics.Activity` (OpenTelemetry), added by the logger when an activity exists
  ([#222](https://github.com/Arc4u-org/Arc4u/issues/222)).
- `Arc4u.Standard.NServiceBus`, `Arc4u.Standard.NServiceBus.Core`, `Arc4u.Standard.NServiceBus.RabbitMQ`
  and `Arc4u.Prism.DI.Wpf`: no 9.0 version
  ([#189](https://github.com/Arc4u-org/Arc4u/issues/189)). They are deprecated on NuGet.org
  ([#272](https://github.com/Arc4u-org/Arc4u/issues/272)). In 9, use Dapr pub/sub and `Prism.DryIoc`.
- .NET 8 and .NET 9 targets, and the .NET Standard 2.0 target of every package except
  `Arc4u.Dependency` and `Arc4u.Dependency.Tool`.
- ADAL (`Arc4u.Standard.OAuth2.AspNetCore.Adal`), Protobuf serializers
  (`Arc4u.Standard.Serializer.Protobuf`, `Arc4u.Standard.Serializer.ProtobufV2`) and
  `Arc4u.Standard.Diagnostics.TraceListeners`.
- MSAL desktop support (`Arc4u.Standard.OAuth2.Msal`). See
  [MSAL status](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#msal-status).
- `IContainer`, `IContainerRegistry`, `IContainerResolve`, `ComponentModelContainer`,
  `DependencyContext` and the `Arc4u.Standard.Dependency.ComponentModel` package.
- `Message`, `Messages`, `MessageType`, `LocalizedMessage` and `AppException`, and the
  FluentValidation helpers `ToMessages` and `ToMessageType`.
- `Arc4u.Standard.OAuth2.AspNetCore.Api` (`ServiceAspectAttribute` for controllers).
- `Authentication:ClientSecrets` (`AddSecretAuthentication`, `CredentialSecretTokenProvider`), replaced
  by `Authentication:ClientTokens`.
- `LoggerContext` and the `Log()`-terminated logging API.
- The `DataContractSerializer`, `DataContractJsonSerializer` and `XmlSerializer` string helpers, and
  `Arc4u.Utils.Enum<T>` / `EnumUtil`.
- The AutoMapper dependency of `Arc4u.Diagnostics.Serilog.Sinks.RealmDb`.
- The change-tracking concept, obsolete with a domain-centric approach
  ([#266](https://github.com/Arc4u-org/Arc4u/issues/266)): the `Arc4u.Standard.Data` package
  (`IdEntity`, `PersistEntity`, the audit entities, `EntitySet<TEntity>`, `EntityItem<TEntity>`,
  `RelationAttribute`, `Validate<T>`, `ValidateAll`), `IPersistEntity` and `PersistChange`,
  `ChangeGraphTracker` and `PersisteChangeExtension` in `Arc4u.EfCore`, and the `IsInsert`,
  `IsUpdate`, `IsDelete`, `IsNone` rules and `ValidatorPredicates` in `Arc4u.FluentValidation`.
  `Graph<T>` and `GraphExtension` are kept. See
  [Arc4u.Data and change tracking removed](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html#arc4udata-and-change-tracking-removed).
- `Arc4u.gRPC`: `ClientErrorInterceptor`. With `Grpc.Net.Client` it converted almost nothing, since a
  call fails when it is awaited, after the interceptor returned. Catch the `RpcException` and use
  `ToResult()` to get the server's ProblemDetails back.
- `UserProfile.Name` (`Domain\SamAccountName`): use `Email`, `PrincipalName`, or `Domain` and
  `SamAccountName` ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- `IScopedLogger<T>` and `ScopedLoggerWrapper<T>`, which existed in the previews only: inject
  `ILogger<T>` ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- `CustomRootCaExtensions.CertificateOptionKeys`: read the names from `IOptions<CARootOptionKeys>`
  ([#226](https://github.com/Arc4u-org/Arc4u/issues/226)).
- `NameClaimType`, `RoleClaimType` and `ValidateAuthority` of `OidcClientAuthenticationOptions` and
  `OidcClientAuthenticationSectionOptions` (`Arc4u.OAuth2.Client.Authentication`), which were never read
  ([#233](https://github.com/Arc4u-org/Arc4u/issues/233)).
- `HybridAuthenticationSectionOptions.OAuth2SettingsKey` and `JwtAuthenticationSectionOptions.ValidateAudience`,
  which were never read: for the audience, use `Authentication:OAuth2.Settings:ValidateAudience`
  ([#220](https://github.com/Arc4u-org/Arc4u/issues/220)).

### Fixed

- Signing out threw a `NotImplementedException` with `AzureADOboTokenProvider` and `ClientTokenProvider`.
  `AzureADOboTokenProvider` now removes from the cache the tokens of every API obtained for the user, also
  with a user access token refreshed since: the index is kept per user (the registered
  `IUserObjectIdentifier`, otherwise the `oid` or `sub` claim) and application (`aud`, otherwise `azp` or
  `appid`), both read from the user token ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `ClientTokenProvider` forgets the token kept in memory at the sign-out, also a token requested before
  it, and renews a token expiring in less than one minute
  ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `BlazorMsalTokenProvider` clears the principal at the sign-out, and `UsernamePasswordTokenProvider` also
  removes the cached user name ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `OidcClientIdentityModelTokenProvider` kept returning the access token after the sign-out. The sign-out
  now clears the tokens in memory, cancels a login in progress and does not keep the tokens of a refresh or
  login in progress. It opens the browser logout only when the user had a session, sends the identity token
  of the login as `id_token_hint` (kept in the new `TokenRefreshInfo.IdentityToken`), bounds the logout by
  `LoginTimeout`, and still logs the user out of the authority when the secure cache fails
  ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `OidcClientIdentityModelTokenProvider`: concurrent calls refresh the token (or log the user in) once,
  since with refresh token rotation a second refresh failed and started an interactive login. The calls
  waiting for a login get its result: when the user cancels it, they fail instead of opening the browser
  again. `LoginTimeout` (5 minutes) bounds a login the user does not complete and the calls waiting for it
  ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `OidcClientIdentityModelTokenProvider`: a network error during a refresh keeps the session (the access
  token is still returned until it expires); only `invalid_grant`, `invalid_scope` and the interaction
  errors log the user in again ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- `OidcClientIdentityModelTokenProvider` and its secure cache: an entry that cannot be decoded is removed
  and the user logs in again, instead of making every call fail; a transient read failure fails the call
  and keeps the entry; an inaccessible cache, or a serializer that does not support `TokenRefreshInfo`,
  keeps the entry and logs the user in; a failing write no longer fails the call that obtained the tokens.
  `SecureCacheTimeout` (30 seconds) bounds the secure-cache operations of a sign-out or a token write, and
  the failures are logged with an optional logger ([#113](https://github.com/Arc4u-org/Arc4u/issues/113)).
- The access token was not in the `BootstrapContext` of the identity in a cookie session, so the `Bootstrap`
  token provider and code written for bearer tokens did not work without `UseOpenIdBearerInjector()`.
  `StandardCookieEvents` now puts it there on each request, after refreshing it when needed (#90, #158).
- With `UseOpenIdBearerInjector()` and on-behalf-of settings, `AzureADOboTokenProvider` used the
  on-behalf-of token that the injector puts in the `BootstrapContext` as the user's token: another
  on-behalf-of exchange in the same request sent a token whose audience is not the application, and the
  sign-out looked for the cached tokens of the user in another index, so the token of the injector stayed
  in the cache. In a cookie session it now uses the access token of the cookie (`TokenRefreshInfo`), and
  the `BootstrapContext` only when there is none ([#275](https://github.com/Arc4u-org/Arc4u/issues/275)).
- Blazor Interactive Server: the `AppPrincipal` of a circuit was set once, when the circuit connected,
  and never followed its authentication state. When the circuit reconnected with another user or without
  a user (for example after a sign-out in another tab), or a revalidating provider turned the state
  anonymous, the event handlers kept acting as the previous user. `ApplicationContextCircuitHandler` and
  `AppPrincipalServerAuthenticationStateProvider` now update the `IApplicationContext` of the circuit on
  `AuthenticationStateChanged`, and clear its principal for an anonymous user
  ([#171](https://github.com/Arc4u-org/Arc4u/issues/171)).
- `OidcTokenProvider` and `RefreshTokenProvider` threw a `NullReferenceException` when the user was not
  logged in yet (the `AccessToken` and `RefreshToken` of `TokenRefreshInfo` are null). `OidcTokenProvider`
  now returns a failed result, or refreshes when only the refresh token is there, and `RefreshTokenProvider`
  throws an `InvalidOperationException` (#161).
- JWT bearer events missing in hybrid authentication.
- `TimeZoneContext` conversion from a specific local time.
- Anonymous calls through service discovery.
- Scope injection in `ILogger<T>`.
- `FlagsEnum.TryPowerOfTwo` and `TryPowerOfTwoExponent` accepted values that are not powers of two,
  so `FlagValues<T>()` also returned `None` and composite values
  ([#261](https://github.com/Arc4u-org/Arc4u/issues/261)).
- `MemoryLogStore.GetLogs` never matched a criteria against a message with capital letters; the
  comparison now ignores case. Reading the memory log store while the sink wrote to it could throw
  `Collection was modified` ([#238](https://github.com/Arc4u-org/Arc4u/issues/238)).
- `RealmLoggingDbCtx`, the singleton `ILogStore` registered by `AddRealmDBLog`, opened its `Realm` in
  its constructor, so `GetLogs` and `RemoveAll` threw `Realm accessed from incorrect thread` from any
  other thread (every ASP.NET Core request). It now opens a `Realm` for each call. The constructor no
  longer takes the unused `ILoggerFactory`, and the `Realm` property is removed
  ([#239](https://github.com/Arc4u-org/Arc4u/issues/239)).
- `RealmDBSink` opened a `Realm` for every batch and never closed it, so instances and their native
  handles piled up until the garbage collector finalized them, and it wrote each event in its own
  transaction. It now opens and closes one `Realm` per batch, without a synchronization context, and
  writes the batch in one transaction
  ([#267](https://github.com/Arc4u-org/Arc4u/issues/267)).
- `StandardBearerEvents.Challenge` answered a 401 with a `ProblemDetails` body saying `"status": 403`
  and the `application/json` content type; the body now says 401 and the content type is
  `application/problem+json` ([#221](https://github.com/Arc4u-org/Arc4u/issues/221)).
- `WriteTo.CategoryFilter` forwarded no event, and `SimpleTextFormatter`, `MemoryLogDB` and `RealmDB`
  labelled every entry `Technical`: they read the `Category` property as a number while the logger
  writes it as a string. The new `Helper.GetCategory` reads the enum, its name, a registered name or
  its number, and keeps a category defined by the application instead of replacing it with
  `Technical`; `SimpleTextFormatter` writes such a category instead of failing
  ([#235](https://github.com/Arc4u-org/Arc4u/issues/235)).
- The properties added with `Add` (and the message template arguments) were kept by the logger and
  written again with later entries; they are now written with that entry only
  ([#273](https://github.com/Arc4u-org/Arc4u/issues/273)).
- The Arc4u logger dropped the `EventId` of every entry; it is now forwarded to the logging providers
  ([#236](https://github.com/Arc4u-org/Arc4u/issues/236)).
- `ILogger<T>` always used the `IAddPropertiesToLog` registered under the key `Transient`, so the
  `Identity` property of `DefaultLoggingProperties` (key `Scoped`) was never added to the log entries
  ([#237](https://github.com/Arc4u-org/Arc4u/issues/237)).
- `FluentLogger` logged the errors other than a `ValidationError` or an exceptional error at the `Error`
  level whatever the level given to `LogIfFailed`, and nothing gave it to FluentResults (`Result.Setup`),
  so `LogIfFailed` logged nothing; `AddResultLogger` and `UseResultLogger` now do it
  ([#251](https://github.com/Arc4u-org/Arc4u/issues/251)).
- `ToGenericMessage` ignored `unexpectedType` in three of its four overloads
  ([#250](https://github.com/Arc4u-org/Arc4u/issues/250)).
- A bearer token issued for another audience is answered with a `detail` that names the rejected audience,
  and the expected audiences are logged ([#35](https://github.com/Arc4u-org/Arc4u/issues/35)).
- `AddJwtAuthentication` and the bearer scheme of `AddHybridAuthentication`: a token without the claim
  that identifies the user, when the claims filler is enabled, gave a 500 error instead of a 401 ([#224](https://github.com/Arc4u-org/Arc4u/issues/224)).
- `UseResourcesRightValidationFor` never detected a missing section
  ([#223](https://github.com/Arc4u-org/Arc4u/issues/223)).
- `ServerPrincipalCache.Put(key, timeout, value, isSlided)` cached the `isSlided` flag instead of the
  principal ([#219](https://github.com/Arc4u-org/Arc4u/issues/219)).
- `AddContextToPrincipalMiddleware` threw when no activity was running, for example in integration tests
  ([#124](https://github.com/Arc4u-org/Arc4u/issues/124)).
- `RemoteClientSecretTokenProvider` sent `user:password` without the Base64 encoding that the `Basic`
  scheme requires ([#121](https://github.com/Arc4u-org/Arc4u/issues/121)).
- The on-behalf-of token was sent in an `access_token` header instead of `Authorization: Bearer` with the
  default `AuthenticationType` ([#229](https://github.com/Arc4u-org/Arc4u/issues/229)).
- The keyed `IKeyValueSettings` were never registered, so the `JwtHttpHandler` constructors that take a
  settings name (`Arc4u.OAuth2.Client.Authentication`, `Arc4u.OAuth.Msal`) always threw,
  `AppPrincipalFactory.CreatePrincipalAsync()` and `CreatePrincipalAsync(settingsResolveName)` always
  returned a failed result, and
  `AppPrincipalFactory.SignOutUserAsync(CancellationToken)` always threw
  ([#231](https://github.com/Arc4u-org/Arc4u/issues/231)).
- Token cache keys depended on the process (`string.GetHashCode`), and the `ClientCredentials` keys
  ignored the extra parameters ([#232](https://github.com/Arc4u-org/Arc4u/issues/232)).
- `BlazorMsalTokenProvider` sends the user to the login page when MSAL cannot renew the token silently,
  instead of failing until the browser is restarted, and `BlazorTokenProvider` no longer reuses a token
  up to 5 minutes after it expired ([#86](https://github.com/Arc4u-org/Arc4u/issues/86)).
- `ToProblemDetails` called on a successful result no longer adds an error to that result,
  `ValidationError.WithMetadata` keeps the first value of a duplicate key instead of throwing, and the
  `IsUtcDateTime` and `IsDateOnly` rules have a default message
  ([#252](https://github.com/Arc4u-org/Arc4u/issues/252)).
- `BlazorController` redirected to the static asset path used before the package rename, and
  `CreatePopupWindow.js` opened `authenticate.html` while the file is `Authenticate.html`, so the page
  was not found on a case-sensitive file system
  ([#227](https://github.com/Arc4u-org/Arc4u/issues/227)).
- The default section of `ReadAuthenticationCookieSettingsOption` (`Arc4u.OAuth2.Blazor`) is
  `Authentication:OAuth2.Settings`, as for `AddAuthenticationCookie`, instead of
  `Authentication:OAuth.Settings` ([#226](https://github.com/Arc4u-org/Arc4u/issues/226)).
- `AddCacheContext` matched `Kind` case-insensitively, but the `CacheContext` it registered resolved it
  case-sensitively and always read the `Caching` section, whatever `sectionName`; nothing registered the
  `ICache` of each kind, which `AddMemoryCacheKind()`, `AddRedisCacheKinds()`, `AddSqlCacheKind()` and
  `AddDaprCacheKind()` now do ([#242](https://github.com/Arc4u-org/Arc4u/issues/242)).
- The first access to a cache configured with `IsAutoStart: false` threw an `InvalidOperationException`
  ([#240](https://github.com/Arc4u-org/Arc4u/issues/240)).
- A memory cache declared without a `Settings` section had a size limit of 100 bytes instead of 100 MB;
  `MemoryCacheOption.SizeLimitInMB` now always holds megabytes
  ([#241](https://github.com/Arc4u-org/Arc4u/issues/241)).
- `AddMongoDatabase` lost the connection-string options that it did not copy one by one (for example
  `directConnection` or `compressors`): the settings are now created from the connection string.
  `DefaultMongoClientFactory.CreateClient` throws a `MongoClientException` when no settings are
  registered for the database, instead of connecting to `localhost:27017`
  ([#254](https://github.com/Arc4u-org/Arc4u/issues/254)).
- `GraphExtension.ApplyReferences(graph, query, path)` always threw, and matched include paths by prefix
  instead of by segment ([#253](https://github.com/Arc4u-org/Arc4u/issues/253)).
- Configuration store: after `ResetAsync`, the first poll published an empty data set
  ([#244](https://github.com/Arc4u-org/Arc4u/issues/244)); a concurrent insert of the initial sections
  by another instance is accepted, insert errors are no longer swallowed, and the section store
  service no longer needs `AddILogger()` (the host failed to start with `Bad Arc4u usage.`)
  ([#245](https://github.com/Arc4u-org/Arc4u/issues/245)).
- The secret decryptor read the earlier configuration sources again, so stream sources failed and remote
  providers were loaded twice ([#246](https://github.com/Arc4u-org/Arc4u/issues/246)).
- Secret decryption with an ECDSA certificate failed silently or with unrelated exceptions; a certificate
  without an RSA key now throws a `NotSupportedException`
  ([#247](https://github.com/Arc4u-org/Arc4u/issues/247)), and a private key that cannot be read gives
  an explicit error ([#40](https://github.com/Arc4u-org/Arc4u/issues/40)).
- `Arc4u.Dependency.Tool`: `[Shared]` with `[Scoped]` gave different lifetimes in the two generators
  ([#257](https://github.com/Arc4u-org/Arc4u/issues/257)); such a class is now registered as scoped and
  reported as `ARC4UDEP005`. A project without a `.cs` file at its root got no `RegisterTypes` method
  ([#258](https://github.com/Arc4u-org/Arc4u/issues/258)).

### Security

- Vulnerable dependencies upgraded ([#130](https://github.com/Arc4u-org/Arc4u/issues/130)).
- `UseResourcesRightValidationFor` and `AddValidateSwaggerRightFor` only protected the exact path, so a user
  without the right got the OpenAPI document by changing its extension: Swashbuckle serves it for
  `swagger.yaml` and `swagger.yml`, and up to 6.5 for any extension (`swagger.jsonx`). The same path with
  another extension is now protected too ([#112](https://github.com/Arc4u-org/Arc4u/issues/112)).
- `CypherCodec` encrypts with AES-GCM and a random nonce for each value (`GenerateKey`, `Encrypt`,
  `Decrypt`), so a modified value or a wrong key is detected. `GenerateKeyAndIV`, `EncodeClearText`,
  `DecodeCypherstring` and `CypherCodecConfig.IV` are removed: the `EncryptionRijndael` section only has a
  `Key`. Long values encrypted with a certificate now have the form `<key>.<data>`. Values encrypted
  with 8.x must be encrypted again ([#114](https://github.com/Arc4u-org/Arc4u/issues/114)).
- The authentication cookie of `AddOidcAuthentication` and `AddHybridAuthentication`, with or without a
  ticket store, is always `Secure`, `HttpOnly` and `SameSite=Lax`. It was only `Secure` when the request
  was in HTTPS, so not behind a proxy that ends TLS ([#110](https://github.com/Arc4u-org/Arc4u/issues/110)).
- `Arc4u.Caching.SqlServer`: `System.Security.Cryptography.Pkcs`, a dependency of
  `Microsoft.Data.SqlClient`, raised from 9.0.18 to 9.0.21 (CVE-2026-84008, high;
  CVE-2026-70162, moderate; both denial of service).

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
  longer published as stable versions; their last stable version is 8.2.1 (8.3.0 previews exist).

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

### Removed

- `net7.0` target.

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

- `Arc4u.Standard.OAuth2.AspNetCore.Msal`, replaced by `Arc4u.Standard.OAuth2.AspNetCore.Authentication`.
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
