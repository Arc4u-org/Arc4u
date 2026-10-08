using Arc4u.Data;
using Arc4u.EfCore;
using Arc4u.UnitTest.Database.EfCore.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arc4u.UnitTest.Database.EfCore;

[Trait("Category", "CI")]
public class GraphTrackingTests
{
    private sealed class OrderContext(DbContextOptions<OrderContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.Id).ValueGeneratedNever();
                e.Ignore(o => o.PersistChange);
                e.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
            });
            modelBuilder.Entity<OrderLine>(e =>
            {
                e.HasKey(l => l.Id);
                e.Property(l => l.Id).ValueGeneratedNever();
                e.Ignore(l => l.PersistChange);
            });
        }
    }

    private static OrderContext CreateContext(string name)
        => new(new DbContextOptionsBuilder<OrderContext>().UseInMemoryDatabase(name).Options);

    private static async Task<Guid> SeedAsync(string name)
    {
        await using var db = CreateContext(name);
        var order = new Order { Reference = "R1", PersistChange = PersistChange.Insert };
        order.Lines.Add(new OrderLine { Label = "L1", PersistChange = PersistChange.Insert });
        db.ChangeTracker.TrackPersistGraph(order);
        await db.SavePersistChangesAsync();
        return order.Id;
    }

    [Fact]
    public async Task NewChildUnderTrackedRootIsAdded()
    {
        var name = Guid.NewGuid().ToString();
        var id = await SeedAsync(name);

        await using var db = CreateContext(name);
        var order = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id);   // tracked

        var line = new OrderLine { Label = "L2", PersistChange = PersistChange.Insert };
        order.Lines.Add(line);

        db.ChangeTracker.TrackPersistGraph(order);

        Assert.Equal(EntityState.Added, db.Entry(line).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(order).State);

        await db.SavePersistChangesAsync();

        await using var check = CreateContext(name);
        Assert.Equal(2, (await check.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id)).Lines.Count);
    }

    [Fact]
    public async Task TrackedEntitiesFollowTheirPersistChange()
    {
        var name = Guid.NewGuid().ToString();
        var id = await SeedAsync(name);

        await using var db = CreateContext(name);
        var order = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id);   // tracked

        order.Reference = "R2";
        order.PersistChange = PersistChange.Update;
        order.Lines[0].PersistChange = PersistChange.Delete;

        db.ChangeTracker.TrackPersistGraph(order);

        Assert.Equal(EntityState.Modified, db.Entry(order).State);
        Assert.Equal(EntityState.Deleted, db.Entry(order.Lines[0]).State);

        await db.SavePersistChangesAsync();

        await using var check = CreateContext(name);
        var saved = await check.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id);
        Assert.Equal("R2", saved.Reference);
        Assert.Empty(saved.Lines);
    }

    [Fact]
    public async Task PersistChangeIsResetAfterSave()
    {
        var name = Guid.NewGuid().ToString();

        await using var db = CreateContext(name);
        var order = new Order { Reference = "R1", PersistChange = PersistChange.Insert };
        var line = new OrderLine { Label = "L1", PersistChange = PersistChange.Insert };
        order.Lines.Add(line);

        db.ChangeTracker.TrackPersistGraph(order);
        await db.SavePersistChangesAsync();

        Assert.Equal(PersistChange.None, order.PersistChange);
        Assert.Equal(PersistChange.None, line.PersistChange);

        // An entity reset to None can now be deleted (Insert -> Delete is an invalid transition).
        line.PersistChange = PersistChange.Delete;
        db.ChangeTracker.TrackPersistGraph(order);
        await db.SavePersistChangesAsync();

        Assert.Equal(PersistChange.None, line.PersistChange);
        await using var check = CreateContext(name);
        Assert.Empty((await check.Orders.Include(o => o.Lines).SingleAsync()).Lines);
    }

    [Fact]
    public async Task PersistChangeIsKeptWhenSaveFails()
    {
        var name = Guid.NewGuid().ToString();
        var id = await SeedAsync(name);

        await using var db = CreateContext(name);
        var duplicate = new Order { Id = id, Reference = "dup", PersistChange = PersistChange.Insert };
        db.ChangeTracker.TrackPersistGraph(duplicate);

        await Assert.ThrowsAnyAsync<Exception>(() => db.SavePersistChangesAsync());

        Assert.Equal(PersistChange.Insert, duplicate.PersistChange);
    }
}
