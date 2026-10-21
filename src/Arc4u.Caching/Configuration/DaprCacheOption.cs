using System.Text.Json;

namespace Arc4u.Configuration.Dapr;
/// <summary>The options of a Dapr state store cache.</summary>
public class DaprCacheOption
{
    /// <summary>Gets or sets the name of the Dapr state store.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Json options given to the Dapr client to serialize the values. When <see langword="null"/>, the defaults of the Dapr client are used, which
    /// serialize with reflection. In a trimmed or native AOT application, set options whose <see cref="JsonSerializerOptions.TypeInfoResolver"/> is a
    /// <c>JsonSerializerContext</c> of the application that declares the types stored in the cache. It is not read from the configuration.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
