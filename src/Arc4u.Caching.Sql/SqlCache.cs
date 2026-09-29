using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration.Sql;
using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
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

    private readonly ILogger<SqlCache> _logger;
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
        _logger = logger;
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
    public override void Initialize([DisallowNull] string store)
    {
        if (string.IsNullOrEmpty(store))
        {
            NotInitializedReason = "When initializing the Sql cache, the value of the store cannot be an empty string.";
            throw new ArgumentException(NotInitializedReason, nameof(store));
        }

        lock (_lock)
        {
            if (IsInitialized)
            {
                _logger.Technical().LogCacheIsAlreadyInitialized(store);
                return;
            }

            try
            {
                Name = store;

                var config = _options.Get(store);

                var option = new SqlServerCacheOptions
                {
                    ConnectionString = config.ConnectionString,
                    TableName = config.TableName,
                    SchemaName = config.SchemaName
                };

                DistributeCache = new SqlServerCache(option);

                if (!string.IsNullOrWhiteSpace(config.SerializerName))
                {
                    IsInitialized = Container.TryGetService<IObjectSerialization>(config.SerializerName!, out var serializerFactory);
                    if (IsInitialized)
                    {
                        SerializerFactory = serializerFactory!;
                    }
                }

                if (!IsInitialized)
                {
                    IsInitialized = Container.TryGetService<IObjectSerialization>(out var serializerFactory);
                    if (IsInitialized)
                    {
                        SerializerFactory = serializerFactory!;
                    }
                }

                if (!IsInitialized)
                {
                    NotInitializedReason = $"Sql Cache {store} is not initialized. An IObjectSerialization instance cannot be resolved via the Ioc.";

                    _logger.Technical().LogError(NotInitializedReason);

                    return;
                }

                _logger.Technical().LogCacheIsInitialized(store);
            }
            catch (Exception ex)
            {
                NotInitializedReason = $"Sql Cache {store} is not initialized. With exception: {ex.Message}";
                throw;
            }
        }
    }
    /// <summary>Returns the name of the cache given to <see cref="Initialize(string)"/>.</summary>
    /// <returns>The name of the cache.</returns>
    /// <exception cref="InvalidOperationException">The cache has not been initialized.</exception>
    public override string ToString() => Name ?? throw new InvalidOperationException("The 'Name' property must not be null.");
}
