using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration.Sql;
using Arc4u.Dependency.Attribute;
using Arc4u.Serializer;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.Caching.Sql;

/// <summary>
/// See Documentation how to create a database here: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-3.0
/// </summary>
[Export("Sql", typeof(ICache))]
public class SqlCache : BaseDistributeCache<SqlCache>, ICache
{
    private string? Name { get; set; }

    /// <summary>
    /// This is a named options => the configuration named instance for this instance will be taken during the intialize method. 
    /// </summary>
    private readonly IOptionsMonitor<SqlCacheOption> _options;

    /// <summary>Initializes a new instance of the <see cref="SqlCache"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="container">The service provider used to resolve the serializer.</param>
    /// <param name="options">The named options of the SQL caches.</param>
    public SqlCache(ILogger<SqlCache> logger, IServiceProvider container, IOptionsMonitor<SqlCacheOption> options) : base(logger, container)
    {
        _options = options;
    }

    /// <summary>
    /// Creates the underlying SQL Server cache from the options named <paramref name="store"/> and resolves the <see cref="IObjectSerialization"/>
    /// (the one registered with <see cref="SqlCacheOption.SerializerName"/> if any, otherwise the default one).
    /// When no serializer can be resolved the cache stays uninitialized and the operations throw <see cref="CacheNotInitializedException"/>.
    /// Calling it again on an initialized cache only logs a warning.
    /// </summary>
    /// <param name="store">The name of the cache, as declared in the configuration.</param>
    /// <exception cref="ArgumentException"><paramref name="store"/> is empty.</exception>
    public override void Initialize([DisallowNull] string store) =>
        InitializeCore(store, "Sql", s =>
        {
            Name = s;

            var config = _options.Get(s);

            var option = new SqlServerCacheOptions
            {
                ConnectionString = config.ConnectionString,
                TableName = config.TableName,
                SchemaName = config.SchemaName
            };

            return (new SqlServerCache(option), config.SerializerName);
        });

    /// <summary>Returns the name of the cache given to <see cref="Initialize(string)"/>.</summary>
    /// <returns>The name of the cache (empty before initialization).</returns>
    public override string ToString() => Name ?? string.Empty;
}
