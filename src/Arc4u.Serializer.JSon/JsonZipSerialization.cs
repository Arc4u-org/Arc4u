using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.IO;

namespace Arc4u.Serializer;

/// <summary>Implements object serialization with JSON stored as a single entry (see <see cref="EntryName"/>) of a ZIP archive.</summary>
public class JsonZipSerialization : JsonCompressedStreamSerializationBase, IObjectSerialization
{
    /// <summary>
    /// Construct an instance with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonZipSerialization()
    {
    }

    /// <summary>
    /// Construct an instance, with specific serializer options
    /// </summary>
    /// <param name="options">Json serializer optios. Default is to use the default options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonZipSerialization(JsonSerializerOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Construct an instance with a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation.</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonZipSerialization(System.Text.Json.Serialization.JsonSerializerContext context)
        : base(context)
    {
    }

    /// <summary>
    /// The name of the entry in the .ZIP file. Default is "content".
    /// </summary>
    protected virtual string EntryName => "content";

    /// <inheritdoc/>
    protected override string SerializerType => "Json+ZipArchive";

    /// <inheritdoc/>
    public byte[] Serialize<T>(T value)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);

        using var output = RecyclableMemoryStreamManager.GetStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(EntryName, CompressionLevel.Fastest);
            using var contentStream = entry.Open();
            InternalSerialize(contentStream, value);
        }
        return output.ToArray();
    }

    /// <summary>Reads the JSON entry of the ZIP archive and deserializes it.</summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="data">The bytes of the ZIP archive.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="InvalidOperationException">The archive has no entry named <see cref="EntryName"/>.</exception>
    public T? Deserialize<T>(byte[] data)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);
        using var compressed = RecyclableMemoryStreamManager.GetStream(data);
        using var archive = new ZipArchive(compressed);
        var entry = archive.GetEntry(EntryName);
        if (null == entry)
        {
            throw new InvalidOperationException($"Zip archive doesn't have an entry {EntryName}");
        }
        using var contentStream = entry.Open();

        return InternalDeserialize<T>(contentStream);
    }

    /// <summary>Reads the JSON entry of the ZIP archive and deserializes it to the given type.</summary>
    /// <param name="data">The bytes of the ZIP archive.</param>
    /// <param name="objectType">The type of the object to create.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">The archive has no entry named <see cref="EntryName"/>.</exception>
    public object? Deserialize(byte[] data, Type objectType)
    {
        Activity.Current?.SetTag("SerializerType", SerializerType);
        using var compressed = RecyclableMemoryStreamManager.GetStream(data);
        using var archive = new ZipArchive(compressed);
        var entry = archive.GetEntry(EntryName);
        if (null == entry)
        {
            throw new InvalidOperationException($"Zip archive doesn't have an entry {EntryName}");
        }
        using var contentStream = entry.Open();
        return InternalDeserialize(contentStream, objectType);
    }
}
