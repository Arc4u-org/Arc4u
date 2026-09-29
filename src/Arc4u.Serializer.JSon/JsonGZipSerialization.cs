using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;

namespace Arc4u.Serializer;

/// <summary>
/// Implement object serialization with Json and GZip compression (https://en.wikipedia.org/wiki/Gzip).
/// </summary>
public class JsonGZipSerialization : JsonCompressedStreamSerialization
{
    /// <summary>
    /// Construct an instance with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonGZipSerialization()
    {
    }

    /// <summary>
    /// Construct an instance, with specific serializer options
    /// </summary>
    /// <param name="options">Json serializer options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonGZipSerialization(JsonSerializerOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Construct an instance with a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation.</param>
    public JsonGZipSerialization(System.Text.Json.Serialization.JsonSerializerContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    protected override string SerializerType => "Json+GZipCompression";

    /// <inheritdoc/>
    protected override Stream CreateForCompression(Stream stream)
    {
        return new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true);
    }

    /// <inheritdoc/>
    protected override Stream CreateForDecompression(Stream stream)
    {
        return new GZipStream(stream, CompressionMode.Decompress);
    }
}
