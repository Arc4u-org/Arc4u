namespace Arc4u.Configuration.Redis;

/// <summary>The options of a Redis Sentinel cache.</summary>
public class RedisSentinelCacheOption
{
    /// <summary>
    /// Name of this cache instance (optional, used for logging or multi-instance scenarios)
    /// </summary>
    public string InstanceName { get; set; } = "";

    /// <summary>
    /// The name of the master in Sentinel configuration
    /// </summary>
    public string MasterName { get; set; } = "mymaster";

    /// <summary>
    /// List of Sentinel endpoints (host:port)
    /// </summary>
    public string[] SentinelEndpoints { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Password for Redis master authentication
    /// </summary>
    public string? RedisPassword { get; set; }

    /// <summary>
    /// Index of the Redis database to use. Default is 0.
    /// </summary>
    public int? DefaultDatabase { get; set; } = 0;

    /// <summary>
    /// Optional serializer name for the cache
    /// </summary>
    public string? SerializerName { get; set; }
}
