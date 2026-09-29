using Arc4u.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Arc4u.EfCore;

/// <summary>
/// Callbacks for <c>ChangeTracker.TrackGraph</c> that set the Entity Framework Core state of every entity of a graph from its <see cref="PersistChange"/>.
/// </summary>
/// <example>
/// <code>
/// context.ChangeTracker.TrackGraph(order, ChangeGraphTracker.Tracker);
/// await context.SaveChangesAsync();
/// </code>
/// </example>
public static class ChangeGraphTracker
{
    /// <summary>
    /// To be used with EfCore ChangeTracker.TrackGraph.
    /// Convert the PersistChange of the PersistEntity to the EfCore state.
    /// </summary>
    /// <param name="node"><see cref="EntityEntryGraphNode"/></param>
    public static void Tracker(EntityEntryGraphNode node)
    {
        node.Entry.State = node.Entry.Entity is IPersistEntity persistEntity ? persistEntity.PersistChange.Convert() : EntityState.Unchanged;
    }

    /// <summary>
    /// To be used with EfCore ChangeTracker.TrackGraph.
    /// Convert the PersistChange of the PersistEntity to the EfCore state.
    /// Here the state will be set based on the rootEntity PersistChange.
    /// </summary>
    /// <param name="rootEntity">The root entity.</param>
    /// <param name="node"><see cref="EntityEntryGraphNode"/></param>
    public static void Tracker(IPersistEntity rootEntity, EntityEntryGraphNode node)
    {
        node.Entry.State = rootEntity.PersistChange.Convert();
    }
}
