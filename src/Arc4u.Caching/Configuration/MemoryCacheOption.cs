namespace Arc4u.Configuration.Memory;
/// <summary>The options of a memory cache.</summary>
public class MemoryCacheOption
{
    /// <summary>Gets or sets the fraction of the entries removed when the size limit is reached. Default is 0.2.</summary>
    public double CompactionPercentage { get; set; } = 0.2;
    /// <summary>Gets or sets the maximum size of the cache, in megabytes. Default is 100.</summary>
    public long SizeLimitInMB { get; set; } = 100;
    /// <summary>Gets or sets the name of the <see cref="Arc4u.Serializer.IObjectSerialization"/> registered with this key to use; when empty or not found, the default serializer is used.</summary>
    public string? SerializerName { get; set; }
}
