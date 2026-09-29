using MongoDB.Driver;

namespace Arc4u.MongoDB;

/// <summary>
/// Define the behavior expected from the factory to create a mongo client and retrieve the database (part of the description).
/// </summary>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public interface IMongoClientFactory<TContext> where TContext : DbContext
{
    /// <summary>
    /// Gets the MongoDB client of the context.
    /// </summary>
    /// <returns>The client.</returns>
    IMongoClient CreateClient();

    /// <summary>
    /// Gets a collection of the database by name.
    /// </summary>
    /// <typeparam name="TEntity">The document type of the collection.</typeparam>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>The collection.</returns>
    IMongoCollection<TEntity> GetCollection<TEntity>(string collectionName);

    /// <summary>
    /// Gets the collection the entity type is mapped to in the <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="TEntity">The document type of the collection.</typeparam>
    /// <returns>The collection.</returns>
    IMongoCollection<TEntity> GetCollection<TEntity>();
}
