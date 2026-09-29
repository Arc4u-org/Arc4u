using FluentResults;
using Microsoft.Extensions.Logging;

namespace Arc4u.Data;

/// <summary>
/// Extension methods validating collections of <see cref="PersistEntity"/>.
/// </summary>
public static class ValidationExtention
{
    /// <summary>
    /// Validates every entity with <see cref="PersistEntity.TryValidate"/> and merges the outcomes in a single result.
    /// </summary>
    /// <remarks>The <paramref name="logger"/> is currently not used.</remarks>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to validate.</param>
    /// <param name="logger">A logger, kept for signature compatibility.</param>
    /// <returns>A result holding the validation errors of all the entities; successful when they are all valid.</returns>
    public static Result ValidateAll<T>(this IEnumerable<T> entities, ILogger<T> logger) where T : PersistEntity
    {
        var result = new Result();
        foreach (var entity in entities)
        {
            result.WithReasons(entity.TryValidate().Reasons);
        }

        return result;
    }
}
