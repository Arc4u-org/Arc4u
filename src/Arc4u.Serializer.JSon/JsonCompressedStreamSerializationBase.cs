using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.IO;

namespace Arc4u.Serializer;

/// <summary>
/// Base class of Json serialization based on compressed streams.
/// </summary>
public abstract class JsonCompressedStreamSerializationBase
{
    private readonly JsonSerializerOptions? _options;
    private readonly System.Text.Json.Serialization.JsonSerializerContext? _context;

    /// <summary>
    /// For performance purposes, we use Microsoft's recyclable MemoryStream pool
    /// </summary>
    private RecyclableMemoryStreamManager? _recyclableMemoryStreamManager;

    /// <summary>
    /// Construct an instance with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    protected JsonCompressedStreamSerializationBase()
    {
    }

    /// <summary>
    /// Construct an instance, optionally specifying compression and other Json serializer options
    /// </summary>
    /// <param name="options">Json serializer options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    protected JsonCompressedStreamSerializationBase(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Construct an instance, optionally specifying compression and a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation</param>
    protected JsonCompressedStreamSerializationBase(System.Text.Json.Serialization.JsonSerializerContext context)
    {
        _context = context;
    }

    /// <summary>Gets the value written in the <c>SerializerType</c> tag of the current <see cref="System.Diagnostics.Activity"/>.</summary>
    protected abstract string SerializerType { get; }

    /// <summary>Gets the pool of recyclable memory streams used to avoid large allocations.</summary>
    protected virtual RecyclableMemoryStreamManager RecyclableMemoryStreamManager => _recyclableMemoryStreamManager ??= new RecyclableMemoryStreamManager();

    /// <summary>Writes the value as UTF-8 JSON to the stream, using the source generation context if one was given, otherwise the serializer options.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="utf8json">The stream that receives the JSON.</param>
    /// <param name="value">The value to serialize.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    protected void InternalSerialize<T>(Stream utf8json, T value)
    {
        if (_context != null)
        {
            JsonSerializer.Serialize(utf8json, value, typeof(T), _context);
        }
        else
        {
            JsonSerializer.Serialize(utf8json, value, _options);
        }
    }

    /// <summary>Reads a value from a UTF-8 JSON stream, using the source generation context if one was given, otherwise the serializer options.</summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="utf8json">The stream that contains the JSON.</param>
    /// <returns>The deserialized value.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    protected T? InternalDeserialize<T>(Stream utf8json)
    {
        if (_context != null)
        {
            return (T?)JsonSerializer.Deserialize(utf8json, typeof(T), _context);
        }
        else
        {
            return JsonSerializer.Deserialize<T>(utf8json, _options);
        }
    }

    /// <summary>Reads a value of the given type from a UTF-8 JSON stream, using the source generation context if one was given, otherwise the serializer options.</summary>
    /// <param name="utf8json">The stream that contains the JSON.</param>
    /// <param name="returnType">The type of the object to create.</param>
    /// <returns>The deserialized object.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    protected object? InternalDeserialize(Stream utf8json, Type returnType)
    {
        if (_context != null)
        {
            return JsonSerializer.Deserialize(utf8json, returnType, _context);
        }
        else
        {
            return JsonSerializer.Deserialize(utf8json, returnType, _options);
        }
    }
}
