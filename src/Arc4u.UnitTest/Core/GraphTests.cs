using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Core;

[Trait("Category", "CI")]
public class GraphTests
{
    public sealed class Product
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Line
    {
        public Product Product { get; set; } = new();
    }

    public sealed class Customer
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Order
    {
        public Customer Customer { get; set; } = new();
        public List<Line> Lines { get; set; } = [];
        public Line[] Archive { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    [Fact]
    public void Include_Should_Accept_References_Collections_And_Arrays()
    {
        var graph = new Graph<Order>()
            .Include(o => o.Customer)
            .Include(o => o.Lines.Select(l => l.Product))
            .Include(o => o.Archive);

        graph.Includes.Should().Equal("Customer", "Lines.Product", "Archive");
        graph.Exist(o => o.Lines).Should().BeTrue();
    }

    [Fact]
    public void Include_Should_Reject_A_Collection_Of_Strings()
    {
        var act = () => new Graph<Order>().Include(o => o.Tags);

        act.Should().Throw<MemberAccessException>();
    }

    [Fact]
    public void Constructor_Should_Validate_The_Paths_Through_Collections()
    {
        new Graph<Order>(["Lines.Product", "Archive.Product", "Customer"]).Includes.Should().HaveCount(3);

        var act = () => new Graph<Order>(["Lines.Missing"]);
        act.Should().Throw<MissingMemberException>();
    }

    [Fact]
    public void RemoveGraph_Should_Keep_The_Other_Paths()
    {
        var graph = new Graph<Order>(["Lines.Product", "Customer"]);

        graph.RemoveGraph(o => o.Lines).Includes.Should().Equal("Customer");
    }

    [Fact]
    public void GetGraph_Should_Return_The_Sub_Paths_Of_A_Reference()
    {
        var graph = new Graph<Line>(["Product"]);

        new Graph<Order>(["Lines.Product"]).GetGraph(o => o.Customer).Includes.Should().BeEmpty();
        graph.GetGraph(l => l.Product).Includes.Should().BeEmpty();
    }
}
