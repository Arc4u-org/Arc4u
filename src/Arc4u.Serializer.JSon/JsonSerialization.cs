using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Arc4u.Serializer;

/// <summary>
/// Implement object serialization with Json
/// </summary>
public class JsonSerialization : IObjectSerialization
{
    private readonly JsonSerializerOptions? _options;
    private readonly System.Text.Json.Serialization.JsonSerializerContext? _context;

    /// <summary>
    /// Construct an instance with default options
    /// </summary>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonSerialization()
    {
    }

    /// <summary>
    /// Construct an instance, with specific serializer options
    /// </summary>
    /// <param name="options">Json serializer options</param>
    [RequiresUnreferencedCode("This constructor is not suitable for AOT. Use the constructor with JsonSerializerContext for AOT compatibility.")]
    public JsonSerialization(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Construct an instance with a serialization context.
    /// This is used for source generation, implemented in .NET 6 or later (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-6-0)
    /// </summary>
    /// <param name="context">Json context for source generation.</param>
    public JsonSerialization(System.Text.Json.Serialization.JsonSerializerContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    public byte[] Serialize<T>(T value)
    {
        Activity.Current?.SetTag("SerializerType", "Json");

        if (_context != null)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), _context);
        }
        else
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    public T? Deserialize<T>(byte[] data)
    {
        Activity.Current?.SetTag("SerializerType", "Json");

        if (_context != null)
        {
            return (T?)JsonSerializer.Deserialize(data, typeof(T), _context);
        }
        else
        {
            return JsonSerializer.Deserialize<T?>(data, _options);
        }
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "The constructor is marked with RequiresUnreferencedCode.")]
    public object? Deserialize(byte[] data, Type objectType)
    {
        Activity.Current?.SetTag("SerializerType", "Json");

        if (_context != null)
        {
            return JsonSerializer.Deserialize(data, objectType, _context);
        }
        else
        {
            return JsonSerializer.Deserialize(data, objectType, _options);
        }
    }
}
