namespace Arc4u.Serializer;

/// <summary>
/// Defines how objects are converted to and from bytes. The Arc4u caches use an implementation to store their values;
/// the implementation is resolved from the container, optionally by the key given in the <c>SerializerName</c> option of the cache.
/// The <c>Arc4u.Serializer.JSon</c> package provides JSON based implementations.
/// </summary>
public interface IObjectSerialization
{
    /// <summary>Serializes a value to bytes.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized bytes.</returns>
    byte[] Serialize<T>(T value);

    /// <summary>Deserializes bytes produced by <see cref="Serialize{T}(T)"/>.</summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="data">The serialized bytes.</param>
    /// <returns>The deserialized value.</returns>
    T? Deserialize<T>(byte[] data);

    /// <summary>Deserializes bytes to an object of a type known only at runtime.</summary>
    /// <param name="data">The serialized bytes.</param>
    /// <param name="objectType">The type of the object to create.</param>
    /// <returns>The deserialized object.</returns>
    object? Deserialize(byte[] data, Type objectType);

}
