---
description: "Definitions of the terms used across the Arc4u documentation."
---
# Glossary

Definitions of the terms used across the Arc4u documentation.

## Application context

The scoped <xref:Arc4u.Security.Principal.IApplicationContext> service that holds the
[AppPrincipal](#appprincipal) and the activity ID of the current request or scope.
`AddApplicationContext` registers it. See
[Server authentication](../guides/authentication-server/index.md).

## AppPrincipal

The Arc4u principal, <xref:Arc4u.Security.Principal.AppPrincipal>: a `ClaimsPrincipal` that
also carries the user's authorization (roles, operations and scopes) and profile. On a server,
it is built from the claims of the authenticated identity. See
[Server authentication](../guides/authentication-server/index.md).

## Business layer

The layer that holds the rules of the application. It is called by the facade and the
interface layer, and calls the data access layer and the service agents. See
[Architecture](architecture.md).

## Cache context

The <xref:Arc4u.Caching.ICacheContext> singleton that gives access to the
[named caches](#named-cache) defined in configuration, by name or through its `Default`
cache. `AddCacheContext` registers it. See [Caching](../guides/caching/index.md).

## Claims filler

An implementation of <xref:Arc4u.Security.Principal.IClaimsFiller> that loads extra claims
for an identity from another source, such as a backend service, while the
[AppPrincipal](#appprincipal) is built. The claims it returns are cached. See
[Server authentication](../guides/authentication-server/index.md).

## Data access layer

A service agent dedicated to databases. It reads and writes the domain model and hides the
database technology from the business layer. See [Architecture](architecture.md) and
[Data access](../guides/data/index.md).

## Domain model

The plain classes (entities and value objects) that the layers of an application exchange.
Facades and interfaces map them to DTOs. See [Architecture](architecture.md).

## Export attribute

The `[Export]` attribute of `Arc4u.Dependency` marks a class to register in dependency
injection, optionally under a key. `[Shared]` makes the registration a singleton and
`[Scoped]` a scoped service; without either, it is transient. The Arc4u source generators
write the registration code. See
[Dependency injection](../guides/dependency-injection/index.md).

## Facade

The service layer dedicated to your own user interfaces. Its REST or gRPC endpoints return
what the screens need and are not versioned, because the UI and its facade are deployed
together. See [Architecture](architecture.md).

## Interface layer

The service layer that other applications call. Unlike the facade, it is versioned: a
contract change is published as a new version while the previous versions keep working.
See [Architecture](architecture.md) and
[gRPC and API versioning](../guides/grpc-versioning/index.md).

## Keyed service

A service registered in the .NET dependency injection container under a key, and resolved
with that key. Arc4u uses keys to select an implementation from configuration, for example
the cache `Kind` or the token provider `ProviderId`. See
[Design principles](design-principles.md) and
[Dependency injection](../guides/dependency-injection/index.md).

## Named cache

A cache defined in the `Caches` list of the `Caching` configuration section, with a `Name`,
a `Kind` (the storage: `Memory`, `Redis`, `RedisSentinel`, `Sql` or `Dapr`) and its
`Settings`. You get it by name from the [cache context](#cache-context). See
[Caching](../guides/caching/index.md).

## ProblemDetails

The standard JSON format for errors returned by HTTP APIs, defined by RFC 9457
(`application/problem+json`). Arc4u converts failed [results](#result-pattern) to
ProblemDetails for REST endpoints, and sends them in the error details of gRPC calls. See
[Results and errors](../guides/results/index.md).

## Result pattern

Returning a `Result` or `Result<T>` from FluentResults, which is either a success with a
value or a failure with errors, instead of throwing exceptions for expected failures.
`Arc4u.Results` adds extension methods and error types for it. See
[Results and errors](../guides/results/index.md).

## Service agent

A component that handles the communication with one external system, such as another
service, files or FTP. It hides the protocol and adds the access token when needed. See
[Architecture](architecture.md) and
[Client authentication and Blazor](../guides/authentication-client/index.md).

## Token provider

An implementation of <xref:Arc4u.OAuth2.Token.ITokenProvider> that obtains an access token,
for example for the current user or for the application itself. Each provider is registered
as a [keyed service](#keyed-service), and the `ProviderId` value of the security settings
selects it. See
[Client authentication and Blazor](../guides/authentication-client/index.md).
