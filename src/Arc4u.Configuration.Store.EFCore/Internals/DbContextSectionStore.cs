using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arc4u.Configuration.Store.EFCore.Internals;

/// <summary>
/// An implementation of a <see cref="ISectionStore"/> by using a <see cref="DbContext"/>.
/// This is expected to be a scoped service.
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
sealed class DbContextSectionStore<TDbContext> : ISectionStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<DbContextSectionStore<TDbContext>> _logger;

    public DbContextSectionStore(TDbContext dbContext, ILogger<DbContextSectionStore<TDbContext>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private DbSet<SectionEntity> SectionEntitySet => _dbContext.Set<SectionEntity>();

    public void Add(IEnumerable<SectionEntity> entities)
    {
        var list = entities.ToList();
        try
        {
            SectionEntitySet.AddRange(list);
            _dbContext.SaveChanges();
        }
        catch (DbUpdateException ex) when (AllKeysExist(list))
        {
            // Another instance inserted the same sections concurrently: the store is in the expected state.
            _logger.LogWarning(ex, "The initial SectionEntities were inserted concurrently by another instance.");
        }
        finally
        {
            // Leave the context clean whether the insert succeeded or not, so a failed insert is not retried by a later SaveChanges.
            foreach (var entity in list)
            {
                _dbContext.Entry(entity).State = EntityState.Detached;
            }
        }
    }

    private bool AllKeysExist(List<SectionEntity> entities)
    {
        var keys = entities.Select(entity => entity.Key).ToList();
        return SectionEntitySet.AsNoTracking().Count(section => keys.Contains(section.Key)) == keys.Count;
    }

    public Task<SectionEntity?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return SectionEntitySet.AsNoTracking().Where(section => section.Key == key).SingleOrDefaultAsync(cancellationToken);
    }

    public List<SectionEntity> GetAll()
    {
        return SectionEntitySet.AsNoTracking().ToList();
    }

    public Task UpdateAsync(IEnumerable<SectionEntity> entities, CancellationToken cancellationToken)
    {
        SectionEntitySet.UpdateRange(entities);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var set = SectionEntitySet;
        set.RemoveRange(await set.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

