# Arc4u

Arc4u is a framework to ease the development of .NET applications. It selects
a number of technologies from the .NET ecosystem and packages them so that
developers can integrate common enterprise concerns without reinventing them.
The framework has been in use for many years and is published as open source.

The `develop/9.0.0` line targets `net10.0` and `net11.0` and ships as a set of
NuGet packages named `Arc4u.*`, each covering one area, for example:

- core abstractions and helpers (`Arc4u`, `Arc4u.Core`, `Arc4u.Threading`)
- dependency injection abstractions (`Arc4u.Dependency`)
- configuration (`Arc4u.Configuration`, `Arc4u.Configuration.Store`)
- caching (`Arc4u.Caching` with Memory, Redis, Sql and Dapr implementations)
- diagnostics and logging (`Arc4u.Diagnostics`, `Arc4u.Diagnostics.Serilog`)
- authentication and authorization with OAuth2 (`Arc4u.OAuth2*`, `Arc4u.Authorization`)
- data access (`Arc4u.Data`, `Arc4u.EfCore`, `Arc4u.MongoDB`, `Arc4u.OData`)
- serialization (`Arc4u.Serializer`, `Arc4u.Serializer.JSon`)
- results and validation (`Arc4u.Results`, `Arc4u.FluentValidation`)
- gRPC and ASP.NET Core integration (`Arc4u.gRPC`, `Arc4u.AspNetCore.*`)

## Where to go next

- [API reference](api/index.md): generated from the XML documentation comments in the source code.
- [Source code and issues](https://github.com/Arc4u-org/Arc4u) on GitHub.

This documentation site is under construction; the guides will be added over time.
