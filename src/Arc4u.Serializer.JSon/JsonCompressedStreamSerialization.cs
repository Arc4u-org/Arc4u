using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.IO;

namespace Arc4u.Serializer;

/// <summary>
/// Base class of Json serialization based on compressed streams.
/// </summary>
public abstract class JsonCompressedStreamSerialization : JsonCompressedStreamSerializationBase, IObjectSerialization
{
    /// <summary>
    /// Construct with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    protected JsonCompressedStreamSerialization()
    {
    }

    /// <summary>
    /// Construct an instance, optionally specifying compression and other Json serializer options
    /// </summary>
    /// <param name="options">Json serializer options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    protected JsonCompressedStreamSerialization(JsonSerializerOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Construct an instance, optionally specifying compression and a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation</param>
    protected JsonCompressedStreamSerialization(System.Text.Json.Serialization.JsonSerializerContext context)
        : base(context)
    {
    }

    /// <summary>Wraps the stream in a compression stream. The underlying stream must be left open.</summary>
    /// <param name="stream">The stream that receives the compressed data.</param>
    /// <returns>The compression stream.</returns>
    protected abstract Stream CreateForCompression(Stream stream);

    /// <summary>Wraps the stream in a decompression stream.</summary>
    /// <param name="stream">The stream that contains the compressed data.</param>
    /// <returns>The decompression stream.</returns>
    protected abstract Stream CreateForDecompression(Stream stream);

    /// <inheritdoc/>
    public byte[] Serialize<T>(T value)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);

        using var output = RecyclableMemoryStreamManager.GetStream();
        using (var compressed = CreateForCompression(output))
        {
            InternalSerialize(compressed, value);
        }

        return output.ToArray();
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] data)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);

        using var compressed = RecyclableMemoryStreamManager.GetStream(data);
        using var uncompressed = CreateForDecompression(compressed);
        return InternalDeserialize<T>(uncompressed);
    }

    /// <inheritdoc/>
    public object? Deserialize(byte[] data, Type objectType)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);

        using var compressed = RecyclableMemoryStreamManager.GetStream(data);
        using var uncompressed = CreateForDecompression(compressed);
        return InternalDeserialize(uncompressed, objectType);
    }
}
