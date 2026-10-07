using System.Linq.Expressions;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arc4u.UnitTest.Database;

[Trait("Category", "CI")]
public class GraphExtensionTests
{
    [Fact]
    public void ApplySingleReferences_ShouldIncludeSingleReferences()
    {
        // Arrange
        var graph = new Graph<TestEntity>(["RelatedEntity"]);
        var queryable = new List<TestEntity>().AsQueryable();

        // Act
        var result = graph.ApplySingleReferences(queryable);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ApplySetReferences_ShouldIncludeAllReferences()
    {
        // Arrange
        var graph = new Graph<TestEntity>(["RelatedEntity", "RelatedEntities"]);
        var queryable = new List<TestEntity>().AsQueryable();

        // Act
        var result = graph.ApplySetReferences(queryable);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyReferences_ShouldThrowExceptionForMultiLevelReference()
    {
        // Arrange
        var graph = new Graph<TestEntity>(["RelatedEntity.RelatedEntities"]);
        var queryable = new List<TestEntity>().AsQueryable();
        Expression<Func<TestEntity, ICollection<TestEntity>>> path = e => e.RelatedEntity.RelatedEntities;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => graph.ApplyReferences(queryable, path));
        Assert.Equal("It is not allowed to check more than one level!", exception.Message);
    }

    [Fact]
    public void ApplyReferences_ShouldIncludeOnlyThePathsStartingWithTheFirstLevelProperty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var seed = new OrderContext(options))
        {
            var product = new Product { Id = 1 };
            seed.Orders.Add(new Order
            {
                Id = 1,
                Customer = new Customer { Id = 1 },
                Lines = { new OrderLine { Id = 1, Product = product } },
                LinesHistory = { new OrderLine { Id = 2, Product = product } },
            });
            seed.SaveChanges();
        }

        var graph = new Graph<Order>(["Customer", "Lines", "Lines.Product", "LinesHistory"]);

        using var db = new OrderContext(options);

        // Act
        var order = graph.ApplyReferences(db.Orders, o => o.Lines).Single();

        // Assert
        var line = Assert.Single(order.Lines);
        Assert.NotNull(line.Product);
        Assert.Null(order.Customer);
        Assert.Empty(order.LinesHistory);
    }

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public TestEntity RelatedEntity { get; set; } = default!;
        public ICollection<TestEntity> RelatedEntities { get; } = [];
    }

    private sealed class Order
    {
        public int Id { get; set; }
        public Customer? Customer { get; set; }
        public ICollection<OrderLine> Lines { get; } = [];
        public ICollection<OrderLine> LinesHistory { get; } = [];
    }

    private sealed class OrderLine
    {
        public int Id { get; set; }
        public Product? Product { get; set; }
    }

    private sealed class Customer
    {
        public int Id { get; set; }
    }

    private sealed class Product
    {
        public int Id { get; set; }
    }

    private sealed class OrderContext(DbContextOptions<OrderContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasMany(o => o.Lines).WithOne().HasForeignKey("OrderId");
            modelBuilder.Entity<Order>().HasMany(o => o.LinesHistory).WithOne().HasForeignKey("HistoryOrderId");
        }
    }
}
