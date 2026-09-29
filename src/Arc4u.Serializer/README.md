# Arc4u.Serializer

The `IObjectSerialization` interface that Arc4u caches use to convert objects to bytes and back. Implementations are in
`Arc4u.Serializer.JSon`.

## Install

```bash
dotnet add package Arc4u.Serializer --prerelease
```

## Usage

```csharp
public class BinaryCodec(IObjectSerialization serializer)
{
    public byte[] Encode<T>(T value) => serializer.Serialize(value);

    public T? Decode<T>(byte[] data) => serializer.Deserialize<T>(data);
}
```

## Documentation

- Guide: [Serialization](https://arc4u-org.github.io/Arc4u/guides/caching/serialization.html)
- API reference: [Arc4u.Serializer](https://arc4u-org.github.io/Arc4u/api/Arc4u.Serializer.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
