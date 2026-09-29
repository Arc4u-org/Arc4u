# Arc4u.Serializer.JSon

System.Text.Json implementations of `IObjectSerialization`: plain JSON, and JSON compressed with GZip, Deflate, Brotli or Zip.

## Install

```bash
dotnet add package Arc4u.Serializer.JSon --prerelease
```

## Usage

```csharp
// Used by the caches that do not name a serializer.
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();

// Used by a cache with "SerializerName": "gzip" in its Settings.
builder.Services.AddKeyedSingleton<IObjectSerialization>("gzip", (_, _) => new JsonGZipSerialization());
```

## Documentation

- Guide: [Serialization](https://arc4u-org.github.io/Arc4u/guides/caching/serialization.html)
- API reference: [Arc4u.Serializer](https://arc4u-org.github.io/Arc4u/api/Arc4u.Serializer.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
