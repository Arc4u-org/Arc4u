namespace Arc4u.MongoDB.Exceptions;

/// <summary>
/// Thrown when an entity type is mapped to several collections and a single one is expected.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class TypeMappedToMoreThanOneCollectionException<TEntity> : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="times">The number of collections the type is mapped to.</param>
    public TypeMappedToMoreThanOneCollectionException(int times) : base($"{times} registration exist for {typeof(TEntity).FullName}.")
    {
    }
}
