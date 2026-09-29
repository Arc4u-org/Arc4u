using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.MongoDB.Configuration;

/// <summary>
/// Builder used by <see cref="DbContext.OnConfiguring(DbContextBuilder)"/> to map entity types to collections.
/// </summary>
public class DbContextBuilder
{
    /// <summary>
    /// Creates a builder for a database.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">The name of the database.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>, or <paramref name="databaseName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseName"/> is empty or white space.</exception>
    public DbContextBuilder(IServiceCollection services, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(databaseName);

        Services = services;
        DatabaseName = databaseName;
        EntityCollectionTypes = [];
    }

    internal readonly IServiceCollection Services;
    internal readonly string DatabaseName;

    internal readonly Dictionary<Type, List<string>> EntityCollectionTypes;

    /// <summary>
    /// Starts the mapping of a collection; follow it with <see cref="WithType.With{TEntity}"/> to say which entity type it holds.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>An object to declare the entity types stored in the collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collectionName"/> is null, empty or white space.</exception>
    public WithType MapCollection(string collectionName)
    {
        return string.IsNullOrWhiteSpace(collectionName)
            ? throw new ArgumentNullException(nameof(collectionName))
            : new WithType(collectionName, this);
    }

}

/// <summary>
/// Second step of the mapping of a collection: declares the entity types it stores.
/// </summary>
public class WithType
{
    /// <summary>
    /// Creates the mapping step for a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="contextBuilder">The builder that records the mapping.</param>
    public WithType(string collectionName, DbContextBuilder contextBuilder)
    {
        _contextBuilder = contextBuilder;
        _collectionName = collectionName;
    }

    private readonly DbContextBuilder _contextBuilder;
    private readonly string _collectionName;

    /// <summary>
    /// Maps the entity type to the collection.
    /// </summary>
    /// <remarks>An entity type can be mapped to several collections by calling <c>MapCollection</c> once per collection; reading it without a collection name then fails. Mapping the same type to the same collection twice throws an <see cref="ArgumentException"/>.</remarks>
    /// <typeparam name="TEntity">The entity type stored in the collection.</typeparam>
    /// <returns>This object, to declare another entity type for the same collection.</returns>
    public WithType With<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);

        // Add if not exist!
        if (!_contextBuilder.EntityCollectionTypes.TryGetValue(type, out var value) ||
            value.Exists(c => c.Equals(_collectionName, StringComparison.OrdinalIgnoreCase)))
        {
            _contextBuilder.EntityCollectionTypes.Add(type, [_collectionName]);

            return this;
        }

        value.Add(_collectionName);
        return this;
    }
}
