using Arc4u.Data;

namespace Arc4u.UnitTest.Database.EfCore.Model;

public class Order : IdEntity
{
    public string Reference { get; set; } = string.Empty;

    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine : IdEntity
{
    public string Label { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public Order? Order { get; set; }
}
