# Arc4u.OData

Makes the URLs in OData responses (`@odata.context`, `@odata.nextLink`) point to the address of a reverse proxy such as YARP instead of the address of the service. It adds two extension methods to `Microsoft.AspNetCore.OData`, it is not an OData implementation.

## Install

```bash
dotnet add package Arc4u.OData --prerelease
```

## Usage

```csharp
using Arc4u.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);
var baseAddress = new Uri("https://gateway.example.com/shop/odata/");   // ends with the route prefix and a slash
var model = new ODataConventionModelBuilder().GetEdmModel();
builder.Services.AddControllers()
    .AddOData(o => o.AddRouteComponents("odata", model, s => s.AddODataSerializerBaseAddress(baseAddress)))
    .AddMvcOptions(o => o.SetODataFormattersBaseAddress(baseAddress));
```

## Documentation

- Guide: [OData](https://arc4u-org.github.io/Arc4u/guides/data/odata.html)
- API reference: [Arc4u.OData](https://arc4u-org.github.io/Arc4u/api/Arc4u.OData.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
