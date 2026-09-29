namespace Arc4u.Configuration.Redis;
/// <summary>The options of a Redis cache.</summary>
public class RedisCacheOption
{
    /// <summary>Gets or sets the StackExchange.Redis connection string.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets the prefix added to every key. Default is <c>Default</c>.</summary>
    public string InstanceName { get; set; } = "Default";
    /// <summary>Gets or sets the name of the <see cref="Arc4u.Serializer.IObjectSerialization"/> registered with this key to use; when empty or not found, the default serializer is used.</summary>
    public string? SerializerName { get; set; }
}
