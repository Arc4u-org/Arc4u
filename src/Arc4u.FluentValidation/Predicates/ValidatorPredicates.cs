using Arc4u.Data;

namespace Arc4u.FluentValidation;

/// <summary>
/// Predicates on the <see cref="IPersistEntity.PersistChange"/> state of an entity, usable with FluentValidation conditions such as <c>When</c>.
/// </summary>
/// <example>
/// <code>
/// RuleFor(o =&gt; o.Name).NotEmpty().When(ValidatorPredicates.IsInsert);
/// </code>
/// </example>
public static class ValidatorPredicates
{
    /// <summary>
    /// Determines whether the entity is marked to be updated.
    /// </summary>
    /// <typeparam name="TElement">The entity type.</typeparam>
    /// <param name="element">The entity to test.</param>
    /// <returns><see langword="true"/> when its <see cref="IPersistEntity.PersistChange"/> is <see cref="PersistChange.Update"/>.</returns>
    public static bool IsUpdate<TElement>(TElement element) where TElement : IPersistEntity
    {
        return element.PersistChange.Equals(PersistChange.Update);
    }
    /// <summary>
    /// Determines whether the entity is marked to be inserted.
    /// </summary>
    /// <typeparam name="TElement">The entity type.</typeparam>
    /// <param name="element">The entity to test.</param>
    /// <returns><see langword="true"/> when its <see cref="IPersistEntity.PersistChange"/> is <see cref="PersistChange.Insert"/>.</returns>
    public static bool IsInsert<TElement>(TElement element) where TElement : IPersistEntity
    {
        return element.PersistChange.Equals(PersistChange.Insert);
    }

    /// <summary>
    /// Determines whether the entity is marked to be deleted.
    /// </summary>
    /// <typeparam name="TElement">The entity type.</typeparam>
    /// <param name="element">The entity to test.</param>
    /// <returns><see langword="true"/> when its <see cref="IPersistEntity.PersistChange"/> is <see cref="PersistChange.Delete"/>.</returns>
    public static bool IsDelete<TElement>(TElement element) where TElement : IPersistEntity
    {
        return element.PersistChange.Equals(PersistChange.Delete);
    }

    /// <summary>
    /// Determines whether the entity has no pending change.
    /// </summary>
    /// <typeparam name="TElement">The entity type.</typeparam>
    /// <param name="element">The entity to test.</param>
    /// <returns><see langword="true"/> when its <see cref="IPersistEntity.PersistChange"/> is <see cref="PersistChange.None"/>.</returns>
    public static bool IsNone<TElement>(TElement element) where TElement : IPersistEntity
    {
        return element.PersistChange.Equals(PersistChange.None);
    }
}
