using System.Text.Json;
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

    // A deeper model: Company -> Departments (list) -> Employees (array) -> Address -> Country,
    // with value type, string and back-reference properties along the way.
    public sealed class Country
    {
        public string Code { get; set; } = string.Empty;
    }

    public sealed class Address
    {
        public string Street { get; set; } = string.Empty;
        public Country Country { get; set; } = null!;
    }

    public sealed class Skill
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Employee
    {
        public int Age { get; set; }
        public Address Address { get; set; } = null!;
        public List<Skill> Skills { get; set; } = null!;
        public Department Department { get; set; } = null!;
    }

    public sealed class Department
    {
        public Employee[] Employees { get; set; } = null!;
        public Employee Manager { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }

    public sealed class Company
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Founded { get; set; }
        public Address Headquarter { get; set; } = null!;
        public List<Department> Departments { get; set; } = null!;
        // Shares a prefix with Departments: paths must be compared on whole segments.
        public Department Department { get; set; } = null!;
    }

    // A base class and a subclass adding a property the graph of the base class cannot include.
    public sealed class Engine
    {
        public string Model { get; set; } = string.Empty;
    }

    public sealed class Trailer
    {
        public int Length { get; set; }
    }

    public class Vehicle
    {
        public Engine Engine { get; set; } = null!;
    }

    public sealed class Truck : Vehicle
    {
        public Trailer Trailer { get; set; } = null!;
    }

    public interface IFleet
    {
        Vehicle Leader { get; }
    }

    public sealed class Fleet : IFleet
    {
        public List<Vehicle> Vehicles { get; set; } = null!;
        public Vehicle[] Reserve { get; set; } = null!;
        public Vehicle Leader { get; set; } = null!;
        public List<string> Plates { get; set; } = null!;
        public List<int> Mileages { get; set; } = null!;
    }

    // An entity that is also a collection of its items, with navigation properties of its own.
    public sealed class Page<TItem> : IEnumerable<TItem>
    {
        public Company Owner { get; set; } = null!;
        public List<TItem> Items { get; set; } = [];
        public IEnumerator<TItem> GetEnumerator() => Items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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

    [Fact]
    public void Include_Should_Build_Deep_Paths_Through_Nested_Selects()
    {
        var graph = new Graph<Company>()
            .Include(c => c.Headquarter.Country)
            .Include(c => c.Departments.Select(d => d.Employees.Select(e => e.Address.Country)))
            .Include(c => c.Departments.Select(d => d.Employees.Select(e => e.Skills)))
            .Include(c => c.Departments.Select(d => d.Manager.Address));

        graph.Includes.Should().Equal(
            "Headquarter.Country",
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager.Address");
    }

    [Fact]
    public void Include_Should_Ignore_A_Path_Already_Included()
    {
        var graph = new Graph<Company>()
            .Include(c => c.Departments.Select(d => d.Manager))
            .Include(c => c.Departments.Select(d => d.Manager));

        graph.Includes.Should().ContainSingle().Which.Should().Be("Departments.Manager");
    }

    [Fact]
    public void Include_Should_Reject_A_String_Or_A_Value_Type_At_The_End_Of_A_Deep_Path()
    {
        var onString = () => new Graph<Company>().Include(c => c.Departments.Select(d => d.Manager.Address.Street));

        onString.Should().Throw<MemberAccessException>();
    }

    [Fact]
    public void Exist_Should_Find_Every_Intermediate_Node_Of_A_Deep_Path()
    {
        var graph = new Graph<Company>(["Departments.Employees.Address.Country"]);

        graph.Exist(c => c.Departments).Should().BeTrue();
        graph.Exist(c => c.Departments.Select(d => d.Employees)).Should().BeTrue();
        graph.Exist(c => c.Departments.Select(d => d.Employees.Select(e => e.Address))).Should().BeTrue();
        graph.Exist(c => c.Departments.Select(d => d.Employees.Select(e => e.Address.Country))).Should().BeTrue();

        graph.Exist(c => c.Headquarter).Should().BeFalse();
        graph.Exist(c => c.Departments.Select(d => d.Manager)).Should().BeFalse();
        graph.Exist(c => c.Departments.Select(d => d.Employees.Select(e => e.Skills))).Should().BeFalse();
    }

    [Fact]
    public void Exist_Should_Not_Match_A_Property_Sharing_A_Name_Prefix()
    {
        var graph = new Graph<Company>(["Departments.Manager"]);

        graph.Exist(c => c.Department).Should().BeFalse();
        graph.Exist(c => c.Departments).Should().BeTrue();
    }

    [Fact]
    public void Exist_Should_Be_Case_Sensitive_Like_GetGraph_And_RemoveGraph()
    {
        // Deserialization does not validate the paths: the only way to get one not cased like the property.
        var copy = JsonSerializer.Deserialize<Graph<Company>>("""{"Includes":["headquarter"]}""")!;

        copy.Includes.Should().Equal("headquarter");
        copy.Exist(c => c.Headquarter).Should().BeFalse();
        copy.RemoveGraph(c => c.Headquarter).Includes.Should().Equal("headquarter");
    }

    [Fact]
    public void Constructor_Should_Validate_Deep_Paths_Mixing_Lists_Arrays_And_References()
    {
        var graph = new Graph<Company>(
        [
            "Headquarter.Country",
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager.Department.Company.Headquarter",
            "Department.Employees.Department",
        ]);

        graph.Includes.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("Departments.Employees.Address.Missing")]
    [InlineData("Departments.Employees.Address.Street")]
    [InlineData("Departments.Employees.Age")]
    [InlineData("Departments.Employees.Skills.Name")]
    [InlineData("Founded")]
    public void Constructor_Should_Reject_An_Invalid_Deep_Path(string path)
    {
        var act = () => new Graph<Company>([path]);

        act.Should().Throw<MissingMemberException>();
    }

    [Fact]
    public void Includes_Setter_Should_Validate_And_Replace_The_Paths()
    {
        var graph = new Graph<Company>(["Headquarter"]);

        graph.Includes = ["Departments.Employees.Skills"];
        graph.Includes.Should().Equal("Departments.Employees.Skills");

        var act = () => graph.Includes = ["Departments.Employees.Name"];
        act.Should().Throw<MissingMemberException>();
        graph.Includes.Should().Equal("Departments.Employees.Skills");
    }

    [Fact]
    public void Includes_Should_Return_A_Copy()
    {
        var graph = new Graph<Company>(["Headquarter"]);

        graph.Includes.Add("Departments");

        graph.Includes.Should().Equal("Headquarter");
    }

    [Fact]
    public void GetGraph_Should_Be_Chainable_Level_By_Level()
    {
        var graph = new Graph<Company>(
        [
            "Department.Manager.Address.Country",
            "Department.Manager.Skills",
            "Department.Company",
            "Headquarter.Country",
        ]);

        var department = graph.GetGraph(c => c.Department);
        department.Includes.Should().Equal("Manager.Address.Country", "Manager.Skills", "Company");

        var manager = department.GetGraph(d => d.Manager);
        manager.Includes.Should().Equal("Address.Country", "Skills");

        var address = manager.GetGraph(e => e.Address);
        address.Includes.Should().Equal("Country");

        address.GetGraph(a => a.Country).Includes.Should().BeEmpty();
    }

    [Fact]
    public void GetGraph_Should_Not_Mix_A_Property_Sharing_A_Name_Prefix()
    {
        var graph = new Graph<Company>(["Departments.Manager", "Department.Company"]);

        graph.GetGraph(c => c.Department).Includes.Should().Equal("Company");
    }

    [Fact]
    public void GetGraph_Should_Return_The_Sub_Paths_Of_A_Collection()
    {
        var graph = new Graph<Company>(
        [
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager",
            "Headquarter",
        ]);

        var departments = graph.GetGraph(c => c.Departments);
        departments.Includes.Should().Equal("Employees.Address.Country", "Employees.Skills", "Manager");
        departments.Exist(d => d.Select(x => x.Employees.Select(e => e.Skills))).Should().BeTrue();
        departments.Exist(d => d.Select(x => x.Company)).Should().BeFalse();

        // Through an array, then a list again.
        var employees = departments.GetGraph(d => d.SelectMany(x => x.Employees));
        employees.Includes.Should().Equal("Address.Country", "Skills");

        employees.GetGraph(e => e.Select(x => x.Address)).Includes.Should().Equal("Country");
        employees.GetGraph(e => e.Select(x => x.Skills)).Includes.Should().BeEmpty();
    }

    [Fact]
    public void Graph_Of_A_Collection_Should_Validate_The_Paths_Against_The_Item_Type()
    {
        new Graph<List<Department>>(["Employees.Address", "Manager"]).Includes.Should().HaveCount(2);
        new Graph<Employee[]>(["Address.Country"]).Includes.Should().HaveCount(1);

        new Graph<List<Department>>()
            .Include(d => d.Select(x => x.Manager.Address))
            .Includes.Should().Equal("Manager.Address");

        new Graph<Department>(["Employees.Address.Country"])
            .GetGraph(d => d.Employees)
            .Includes.Should().Equal("Address.Country");
        new Graph<IEnumerable<Employee[]>>(["Address.Country"]).Includes.Should().HaveCount(1);

        var act = () => new Graph<List<Department>>(["Count"]);
        act.Should().Throw<MissingMemberException>();
    }

    [Fact]
    public void Graph_Of_An_Entity_Implementing_IEnumerable_Should_Keep_Its_Own_Properties()
    {
        // Owner is declared by Page<T> itself, Address by its items.
        var graph = new Graph<Page<Employee>>(["Owner.Headquarter", "Address.Country"]);

        graph.Includes.Should().Equal("Owner.Headquarter", "Address.Country");
    }

    [Fact]
    public void GetGraph_Should_Reject_A_Path_Deeper_Than_One_Level()
    {
        var graph = new Graph<Company>(["Headquarter.Country"]);

        var act = () => graph.GetGraph(c => c.Headquarter.Country);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveGraph_Should_Remove_A_Whole_Deep_Branch_Only()
    {
        var graph = new Graph<Company>(
        [
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager",
            "Headquarter.Country",
        ]);

        graph.RemoveGraph(c => c.Departments.Select(d => d.Employees))
             .Includes.Should().Equal("Departments.Manager", "Headquarter.Country");

        graph.RemoveGraph(c => c.Departments.Select(d => d.Employees.Select(e => e.Address)))
             .Includes.Should().Equal("Departments.Employees.Skills", "Departments.Manager", "Headquarter.Country");

        // The original graph is left untouched.
        graph.Includes.Should().HaveCount(4);
    }

    [Fact]
    public void RemoveGraph_Should_Not_Remove_A_Property_Sharing_A_Name_Prefix()
    {
        var graph = new Graph<Company>(["Departments.Manager", "Department.Company"]);

        graph.RemoveGraph(c => c.Department).Includes.Should().Equal("Departments.Manager");
    }

    private static Company BuildCompany()
    {
        var company = new Company
        {
            Name = "Arc4u",
            Headquarter = new Address { Country = new Country() },
        };

        var employee = new Employee
        {
            Address = new Address { Country = new Country() },
            Skills = [new Skill()],
        };

        var department = new Department
        {
            Employees = [employee],
            Manager = new Employee { Address = new Address() },
        };

        company.Departments = [department];

        return company;
    }

    [Fact]
    public void Parse_Should_Return_The_Leaf_Paths_Of_A_Deep_Instance()
    {
        var graph = Graph<Company>.Parse(BuildCompany());

        graph.Includes.Should().BeEquivalentTo(
            "Headquarter.Country",
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager.Address");
    }

    [Fact]
    public void Parse_Should_Keep_Empty_Collections_Unless_Elements_Are_Required()
    {
        var company = new Company
        {
            Departments = [new Department { Employees = [], Manager = new Employee { Skills = [] } }],
        };

        Graph<Company>.Parse(company).Includes.Should().BeEquivalentTo(
            "Departments.Employees",
            "Departments.Manager.Skills");

        Graph<Company>.Parse(company, shouldHaveElement: true).Includes.Should().BeEquivalentTo(
            "Departments.Manager");
    }

    [Fact]
    public void Parse_Should_Ignore_Null_References_And_Collections()
    {
        Graph<Company>.Parse(new Company()).Includes.Should().BeEmpty();
        Graph<Company>.Parse(new Company(), shouldHaveElement: true).Includes.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_Not_Follow_Back_References_Forever()
    {
        var company = BuildCompany();
        var department = company.Departments[0];
        department.Company = company;
        department.Manager.Department = department;
        department.Employees[0].Department = department;

        var expected = new[]
        {
            "Headquarter.Country",
            "Departments.Employees.Address.Country",
            "Departments.Employees.Skills",
            "Departments.Manager.Address",
        };

        Graph<Company>.Parse(company).Includes.Should().BeEquivalentTo(expected);
        Graph<Company>.Parse(company, shouldHaveElement: true).Includes.Should().BeEquivalentTo(expected);
    }

    private static Truck BuildTruck() => new() { Engine = new Engine(), Trailer = new Trailer() };

    [Fact]
    public void Parse_Should_Follow_The_Declared_Types_And_Not_The_Subclasses()
    {
        var fleet = new Fleet
        {
            Vehicles = [BuildTruck()],
            Reserve = [BuildTruck()],
            Leader = BuildTruck(),
        };

        var expected = new[] { "Vehicles.Engine", "Reserve.Engine", "Leader.Engine" };

        Graph<Fleet>.Parse(fleet).Includes.Should().BeEquivalentTo(expected);
        Graph<Fleet>.Parse(fleet, shouldHaveElement: true).Includes.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_Should_Follow_The_Graph_Type_When_The_Instance_Is_A_Subclass()
    {
        Graph<Vehicle>.Parse(BuildTruck()).Includes.Should().Equal("Engine");
        Graph<Vehicle>.Parse(BuildTruck(), shouldHaveElement: true).Includes.Should().Equal("Engine");
    }

    [Fact]
    public void Parse_Should_Read_The_Properties_Declared_By_An_Interface()
    {
        var fleet = new Fleet { Leader = BuildTruck(), Vehicles = [BuildTruck()] };

        // Vehicles is not part of IFleet.
        Graph<IFleet>.Parse(fleet).Includes.Should().Equal("Leader.Engine");
        Graph<IFleet>.Parse(fleet, shouldHaveElement: true).Includes.Should().Equal("Leader.Engine");
    }

    [Fact]
    public void Parse_Should_Not_Follow_A_Back_Reference_Through_A_List()
    {
        var company = new Company();
        var department = new Department { Company = company };
        company.Departments = [department];

        // Company.Departments starts with the department being parsed: skipped like a back reference through an array or a reference.
        Graph<Department>.Parse(department).Includes.Should().Equal("Company");
        Graph<Department>.Parse(department, shouldHaveElement: true).Includes.Should().Equal("Company");
    }

    [Fact]
    public void Parse_Should_Ignore_Collections_Of_Strings_And_Value_Types()
    {
        var filled = new Fleet { Plates = ["1-ABC-123"], Mileages = [42] };
        var empty = new Fleet { Plates = [], Mileages = [] };

        Graph<Fleet>.Parse(filled).Includes.Should().BeEmpty();
        Graph<Fleet>.Parse(filled, shouldHaveElement: true).Includes.Should().BeEmpty();
        Graph<Fleet>.Parse(empty).Includes.Should().BeEmpty();
        Graph<Fleet>.Parse(empty, shouldHaveElement: true).Includes.Should().BeEmpty();
    }

    [Fact]
    public void Parsed_Graph_Should_Answer_Exist_And_GetGraph()
    {
        var graph = Graph<Company>.Parse(BuildCompany());

        graph.Exist(c => c.Departments.Select(d => d.Employees.Select(e => e.Address.Country))).Should().BeTrue();
        graph.Exist(c => c.Department).Should().BeFalse();
        graph.GetGraph(c => c.Headquarter).Includes.Should().Equal("Country");
    }

    // Members that are not navigation properties: non-public, static, an indexer, and a collection of collections.
    public sealed class Holder
    {
        public static Address Shared { get; } = new();
        public Address Home { get; set; } = null!;
        private Address Hidden { get; set; } = new();
        internal Address Internal { get; set; } = new();
        public List<Employee[]> Shifts { get; set; } = null!;
        public Address this[int index] => Hidden;
    }

    public class BaseHolder
    {
        public Address Place { get; set; } = null!;
    }

    // Hides Place with another type.
    public sealed class DerivedHolder : BaseHolder
    {
        public new Country Place { get; set; } = null!;
    }

    [Fact]
    public void Include_Should_Reject_A_Value_Type_Inside_A_Select()
    {
        var act = () => new Graph<Company>().Include(c => c.Departments.Select(d => d.Employees.Select(e => e.Age)));

        act.Should().Throw<MemberAccessException>();
    }

    [Fact]
    public void Include_Should_Keep_The_Path_Of_A_Filtered_Or_Ordered_Collection()
    {
        var take = 3;

        var graph = new Graph<Company>()
            .Include(c => c.Departments.Where(d => d.Manager != null).OrderBy(d => d.Company.Name).Take(take).Select(d => d.Manager))
            .Include(c => c.Departments.First().Company)
            .Include(c => c.Departments.SelectMany(d => d.Employees).Where(e => e.Age > 30).Select(e => e.Skills));

        graph.Includes.Should().Equal("Departments.Manager", "Departments.Company", "Departments.Employees.Skills");
    }

    [Fact]
    public void Parse_Should_Only_Follow_Public_Instance_Properties()
    {
        var holder = new Holder { Home = new Address(), Shifts = [[new Employee()]] };

        Graph<Holder>.Parse(holder).Includes.Should().Equal("Home");
        Graph<Holder>.Parse(holder, shouldHaveElement: true).Includes.Should().Equal("Home");
    }

    [Fact]
    public void Graph_Should_Pick_The_Property_Hiding_A_Base_Property()
    {
        new Graph<DerivedHolder>(["Place"]).Includes.Should().Equal("Place");
        Graph<DerivedHolder>.Parse(new DerivedHolder { Place = new Country() }).Includes.Should().Equal("Place");

        // Place is a Country here, which has no Country property.
        var act = () => new Graph<DerivedHolder>(["Place.Country"]);
        act.Should().Throw<MissingMemberException>();
    }

    [Fact]
    public void Collections_Of_Collections_Should_Not_Be_Included()
    {
        var include = () => new Graph<Holder>().Include(h => h.Shifts);
        var constructor = () => new Graph<Holder>(["Shifts"]);

        include.Should().Throw<MemberAccessException>();
        constructor.Should().Throw<MissingMemberException>();
    }

    [Fact]
    public void Constructor_Should_Report_A_String_As_A_Simple_Type()
    {
        var act = () => new Graph<Company>(["Headquarter.Street"]);

        act.Should().Throw<MissingMemberException>().WithMessage("*Street is a simple type*");
    }

    [Fact]
    public void Deserialized_Graph_Should_Not_Keep_Null_Includes()
    {
        var graph = JsonSerializer.Deserialize<Graph<Company>>("""{"Includes":null}""")!;

        graph.Includes.Should().BeEmpty();
        graph.Include(c => c.Headquarter).Includes.Should().Equal("Headquarter");
    }

    [Fact]
    public void Deserialization_Should_Skip_Unknown_Properties_And_Follow_The_Case_Option()
    {
        JsonSerializer.Deserialize<Graph<Company>>("""{"Version":2,"Extra":{"A":[1]},"Includes":["Headquarter"]}""")!
                      .Includes.Should().Equal("Headquarter");

        JsonSerializer.Deserialize<Graph<Company>>("""{"includes":["Headquarter"]}""")!.Includes.Should().BeEmpty();
        JsonSerializer.Deserialize<Graph<Company>>("""{"includes":["Headquarter"]}""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
                      .Includes.Should().Equal("Headquarter");
    }

    [Theory]
    [InlineData("""["Headquarter"]""")]
    [InlineData("""{"Includes":"Headquarter"}""")]
    [InlineData("""{"Includes":[1]}""")]
    public void Deserialization_Should_Reject_A_Malformed_Graph(string json)
    {
        var act = () => JsonSerializer.Deserialize<Graph<Company>>(json);

        act.Should().Throw<JsonException>();
    }
}
