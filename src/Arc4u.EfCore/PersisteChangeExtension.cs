using Microsoft.EntityFrameworkCore;

namespace Arc4u.EfCore;

/// <summary>
/// Conversion between the Arc4u <see cref="Arc4u.Data.PersistChange"/> and the Entity Framework Core <see cref="EntityState"/>.
/// </summary>
public static class PersisteChangeExtension
{
    /// <summary>
    /// Converts a <see cref="Arc4u.Data.PersistChange"/> to an <see cref="EntityState"/>.
    /// </summary>
    /// <param name="persist">The change to persist.</param>
    /// <returns><see cref="EntityState.Deleted"/> for <c>Delete</c>, <see cref="EntityState.Modified"/> for <c>Update</c>, <see cref="EntityState.Added"/> for <c>Insert</c>, otherwise <see cref="EntityState.Unchanged"/>.</returns>
    public static EntityState Convert(this Arc4u.Data.PersistChange persist)
    {
        switch (persist)
        {
            case Arc4u.Data.PersistChange.Delete:
                return EntityState.Deleted;
            case Arc4u.Data.PersistChange.Update:
                return EntityState.Modified;
            case Arc4u.Data.PersistChange.Insert:
                return EntityState.Added;
        }

        return EntityState.Unchanged;
    }
}
