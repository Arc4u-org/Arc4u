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

    /// <summary>
    /// Sets the Entity Framework Core state of every entity reachable from <paramref name="rootEntity"/> from its <see cref="PersistChange"/>,
    /// including the entities the context already tracks.
    /// </summary>
    /// <remarks>
    /// Unlike <c>ChangeTracker.TrackGraph(root, ChangeGraphTracker.Tracker)</c>, the traversal goes through the entities that are already tracked,
    /// so a new child (<see cref="PersistChange.Insert"/>) added under a tracked root is <see cref="EntityState.Added"/>.
    /// An entity not tracked yet gets the state of its <see cref="PersistChange"/> (<see cref="EntityState.Unchanged"/> when it does not implement <see cref="IPersistEntity"/>).
    /// An entity already tracked keeps its state when its <see cref="PersistChange"/> is <see cref="PersistChange.None"/>; otherwise it gets the state of its <see cref="PersistChange"/>.
    /// </remarks>
    /// <param name="changeTracker">The change tracker of the context.</param>
    /// <param name="rootEntity">The root of the graph.</param>
    /// <exception cref="ArgumentNullException"><paramref name="changeTracker"/> or <paramref name="rootEntity"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// context.ChangeTracker.TrackPersistGraph(order);
    /// await context.SavePersistChangesAsync();
    /// </code>
    /// </example>
    public static void TrackPersistGraph(this ChangeTracker changeTracker, object rootEntity)
    {
        ArgumentNullException.ThrowIfNull(changeTracker);
        ArgumentNullException.ThrowIfNull(rootEntity);

        // This overload of TrackGraph also visits the tracked entities; the visited set stops the traversal on cycles (back navigations).
        changeTracker.TrackGraph(rootEntity, new HashSet<object>(ReferenceEqualityComparer.Instance), node =>
        {
            if (!node.NodeState.Add(node.Entry.Entity))
            {
                return false;
            }

            if (node.Entry.State == EntityState.Detached)
            {
                Tracker(node);
            }
            else if (node.Entry.Entity is IPersistEntity { PersistChange: not PersistChange.None } persistEntity)
            {
                node.Entry.State = persistEntity.PersistChange.Convert();
            }

            return true;
        });
    }

    /// <summary>
    /// Saves the changes of the context and, when the save succeeds, sets the <see cref="PersistChange"/> of the saved entities back to <see cref="PersistChange.None"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">A token to cancel the save.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static async Task<int> SavePersistChangesAsync(this DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Collected before saving: the deleted entities are detached by the save.
        var entities = GetChangedPersistEntities(context);

        var count = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ResetPersistChange(entities);

        return count;
    }

    /// <summary>
    /// Saves the changes of the context and, when the save succeeds, sets the <see cref="PersistChange"/> of the saved entities back to <see cref="PersistChange.None"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static int SavePersistChanges(this DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entities = GetChangedPersistEntities(context);

        var count = context.SaveChanges();

        ResetPersistChange(entities);

        return count;
    }

    private static List<IPersistEntity> GetChangedPersistEntities(DbContext context)
    {
        return context.ChangeTracker.Entries()
                      .Select(e => e.Entity)
                      .OfType<IPersistEntity>()
                      .Where(e => e.PersistChange != PersistChange.None)
                      .ToList();
    }

    private static void ResetPersistChange(List<IPersistEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.PersistChange = PersistChange.None;
        }
    }
}
