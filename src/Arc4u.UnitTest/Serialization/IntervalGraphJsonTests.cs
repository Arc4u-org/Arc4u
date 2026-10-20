using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.UnitTest.Core;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Serialization;

[JsonSerializable(typeof(Bound<int>))]
[JsonSerializable(typeof(Interval<int>))]
[JsonSerializable(typeof(Interval<string>))]
[JsonSerializable(typeof(Interval<int?>))]
[JsonSerializable(typeof(Period))]
[JsonSerializable(typeof(Graph<GraphTests.Company>))]
internal sealed partial class IntervalGraphJsonContext : JsonSerializerContext
{
}

[Trait("Category", "CI")]
public class IntervalGraphJsonTests
{
    public static TheoryData<bool> SourceGenerated => new() { false, true };

    private static T RoundTrip<T>(T value, bool sourceGenerated)
    {
        if (sourceGenerated)
        {
            var typeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)IntervalGraphJsonContext.Default.GetTypeInfo(typeof(T))!;
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(value, typeInfo), typeInfo)!;
        }

        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Bound_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var bound = new Bound<int>(BoundType.Upper, BoundDirection.Closed, 42);

        var copy = RoundTrip(bound, sourceGenerated);

        copy.Should().Be(bound);
        copy.Type.Should().Be(BoundType.Upper);
        copy.Direction.Should().Be(BoundDirection.Closed);
        copy.Value.Should().Be(42);
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Interval_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var interval = new Interval<int>(BoundDirection.Closed, 5, 10, BoundDirection.Opened);

        var copy = RoundTrip(interval, sourceGenerated);

        copy.Should().Be(interval);
        copy.LowerBound.Should().Be(interval.LowerBound);
        copy.UpperBound.Should().Be(interval.UpperBound);
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Universe_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var copy = RoundTrip(Interval.Universe<int>(), sourceGenerated);

        copy.IsUniverse.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Empty_Interval_With_Closed_Infinity_Bounds_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        // The empty interval of a reference type has closed infinity bounds, which the public Bound constructor rejects.
        var copy = RoundTrip(Interval.Empty<string>(), sourceGenerated);

        copy.IsEmpty.Should().BeTrue();
        copy.Should().Be(Interval.Empty<string>());
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Period_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var period = new Period(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.FromHours(1)), true, true);

        var copy = RoundTrip(period, sourceGenerated);

        copy.Should().Be(period);
        copy.LowerBound.Value.Should().Be(period.LowerBound.Value);
        copy.UpperBound.Value.Should().Be(period.UpperBound.Value);
        copy.UpperBound.Direction.Should().Be(BoundDirection.Closed);
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Infinite_Period_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var period = new Period(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null);

        var copy = RoundTrip(period, sourceGenerated);

        copy.Should().Be(period);
        copy.UpperBound.Value.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Graph_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        var graph = new Graph<GraphTests.Company>(
        [
            "Departments.Employees.Address.Country",
            "Departments.Manager",
            "Headquarter",
        ]);

        var copy = RoundTrip(graph, sourceGenerated);

        copy.Includes.Should().Equal(graph.Includes);
        copy.GetGraph(c => c.Headquarter).Includes.Should().BeEmpty();
    }

    [Fact]
    public void Graph_Should_Be_Serialized_As_Its_Includes()
    {
        var graph = new Graph<GraphTests.Company>(["Headquarter"]);

        JsonSerializer.Serialize(graph).Should().Be("""{"Includes":["Headquarter"]}""");
    }

    [Theory]
    [MemberData(nameof(SourceGenerated))]
    public void Empty_Interval_Of_A_Nullable_Value_Type_Should_Survive_A_Json_Round_Trip(bool sourceGenerated)
    {
        // Like a reference type, int? has closed infinity bounds in its empty interval.
        var copy = RoundTrip(Interval.Empty<int?>(), sourceGenerated);

        copy.IsEmpty.Should().BeTrue();
        copy.Should().Be(Interval.Empty<int?>());
    }
}
