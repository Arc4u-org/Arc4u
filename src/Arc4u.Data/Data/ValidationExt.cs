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
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to validate.</param>
    /// <param name="logger">The logger receiving one error per validation message.</param>
    /// <returns>A result holding the validation errors of all the entities; successful when they are all valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entities"/> or <paramref name="logger"/> is null.</exception>
    public static Result ValidateAll<T>(this IEnumerable<T> entities, ILogger<T> logger) where T : PersistEntity
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(logger);

        var result = new Result();
        foreach (var entity in entities)
        {
            result.WithReasons(entity.TryValidate().LogErrors(logger, entity.GetType()).Reasons);
        }

        return result;
    }

    internal static Result LogErrors(this Result result, ILogger logger, Type entityType)
    {
        foreach (var error in result.Errors)
        {
            logger.LogError("Validation of {EntityType} failed: {Message}", entityType.Name, error.Message);
        }

        return result;
    }
}
