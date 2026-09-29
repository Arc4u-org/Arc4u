using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;

namespace Arc4u.Serializer;

/// <summary>
/// Implement object serialization with Json and Brotli compression (https://en.wikipedia.org/wiki/Brotli).
/// Only available for .NET Standard 2.1 / .NET Core 3.0+
/// </summary>
public class JsonBrotliSerialization : JsonCompressedStreamSerialization
{
    /// <summary>
    /// Construct an instance with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonBrotliSerialization()
    {
    }

    /// <summary>
    /// Construct an instance, with specific serializer options
    /// </summary>
    /// <param name="options">Json serializer options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonBrotliSerialization(JsonSerializerOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Construct an instance with a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation.</param>
    public JsonBrotliSerialization(System.Text.Json.Serialization.JsonSerializerContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    protected override string SerializerType => "Json+BrotliCompression";

    /// <inheritdoc/>
    protected override Stream CreateForCompression(Stream stream)
    {
        // Be careful when choosing the compression level for Brötli.
        // See https://github.com/dotnet/runtime/issues/26097 and https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#compression. Once we are on .NET 7+, this can be replaced by CompressionLevel.Optimal which will default to 4.
        return new BrotliStream(stream, CompressionLevel.Fastest, leaveOpen: true);
    }

    /// <inheritdoc/>
    protected override Stream CreateForDecompression(Stream stream)
    {
        return new BrotliStream(stream, CompressionMode.Decompress);
    }
}
