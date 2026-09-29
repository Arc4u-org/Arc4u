using Arc4u.MongoDB.Configuration;

namespace Arc4u.MongoDB;

/// <summary>
/// Describes a MongoDB database: its name and the collections that hold each entity type.
/// </summary>
/// <remarks>
/// There is one context per database. Derive from it and map the collections in <see cref="OnConfiguring(DbContextBuilder)"/>, then register it with
/// <c>AddMongoDatabase&lt;TContext&gt;</c>; use <see cref="IMongoClientFactory{TContext}"/> to reach the collections.
/// </remarks>
/// <example>
/// <code>
/// public class ShopContext : DbContext
/// {
///     protected override void OnConfiguring(DbContextBuilder context)
///     {
///         context.MapCollection("orders").With&lt;Order&gt;();
///     }
/// }
/// </code>
/// </example>
public abstract class DbContext
{
    /// <summary>
    /// Maps the collections of the database.
    /// </summary>
    /// <param name="context">The builder used to map an entity type to a collection.</param>
    protected abstract void OnConfiguring(DbContextBuilder context);

    internal void Configure(DbContextBuilder context)
    {
        OnConfiguring(context);

        _databaseName = context.DatabaseName;
        _entityCollectionTypes = context.EntityCollectionTypes;
    }

    private string _databaseName = string.Empty;
    private Dictionary<Type, List<string>> _entityCollectionTypes = [];

    /// <summary>
    /// Gets the name of the database, taken from the connection string or from the name given at registration.
    /// </summary>
    public string DatabaseName { get => _databaseName; }

    /// <summary>
    /// Gets, for each entity type, the names of the collections it is mapped to.
    /// </summary>
    public Dictionary<Type, List<string>> EntityCollectionTypes { get => _entityCollectionTypes; }
}
