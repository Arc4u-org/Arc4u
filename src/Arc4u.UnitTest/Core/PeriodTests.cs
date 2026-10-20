using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Core;

[Trait("Category", "CI")]
public class PeriodTest
{
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        var lower = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var upper = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var period = new Period(lower, upper);

        period.LowerBound.Value.Should().Be(lower);
        period.UpperBound.Value.Should().Be(upper);
        period.LowerBound.Direction.Should().Be(BoundDirection.Closed);
        period.UpperBound.Direction.Should().Be(BoundDirection.Opened);
    }

    [Fact]
    public void Constructor_With_Included_Upper_Should_Set_UpperBound_Closed()
    {
        var lower = DateTimeOffset.Now;
        var upper = lower.AddDays(1);

        var period = new Period(lower, upper, true);

        period.UpperBound.Direction.Should().Be(BoundDirection.Closed);
    }

    [Fact]
    public void Constructor_With_Bounds_Should_Set_Bounds()
    {
        var lower = new Bound<DateTimeOffset?>(BoundType.Lower, BoundDirection.Closed, DateTimeOffset.MinValue);
        var upper = new Bound<DateTimeOffset?>(BoundType.Upper, BoundDirection.Closed, DateTimeOffset.MaxValue);

        var period = new Period(lower, upper);

        period.LowerBound.Should().Be(lower);
        period.UpperBound.Should().Be(upper);
    }

    [Fact]
    public void ToString_Should_Format_Period()
    {
        var lower = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var upper = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var period = new Period(lower, upper);

        var str = period.ToString("yyyy-MM-dd", null);

        str.Should().Contain("2024-01-01").And.Contain("2024-12-31");
    }

    [Fact]
    public void Constructor_With_Null_Bound_Must_Throw_ArgumentNullException()
    {
        var exception = Record.Exception(() => new Period(null!, (Bound<DateTimeOffset?>)null!));

        exception.Should().BeOfType<ArgumentNullException>();
        exception.Message.Should().Be("Value cannot be null. (Parameter 'lowerBound')");
    }

    [Fact]
    public void Constructor_With_Null_Upper_Bound_Must_Throw_ArgumentNullException()
    {
        var lower = new Bound<DateTimeOffset?>(BoundType.Lower, BoundDirection.Closed, DateTimeOffset.MinValue);

        var exception = Record.Exception(() => new Period(lower, null!));

        exception.Should().BeOfType<ArgumentNullException>();
        exception.Message.Should().Be("Value cannot be null. (Parameter 'upperBound')");
    }

    [Fact]
    public void Constructor_With_Null_Upper_DateTimeOffset_Must_Throw_ArgumentException()
    {
        var lowerBound = DateTimeOffset.Now;
        var period = new Period(lowerBound, null);

        period.LowerBound.Direction.Should().Be(BoundDirection.Closed);
        period.LowerBound.Type.Should().Be(BoundType.Lower);
        period.LowerBound.Value.Should().Be(lowerBound);

        period.UpperBound.Direction.Should().Be(BoundDirection.Opened);
        period.UpperBound.Type.Should().Be(BoundType.Upper);
        period.UpperBound.Value.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Null_DateTimeOffset_Must_Throw_ArgumentException()
    {
        var exception = Record.Exception(() => new Period(null, (DateTimeOffset?)null));

        exception.Should().BeOfType<ArgumentException>("An infinity bound must define an opened direction.");
    }

    [Fact]
    public void IsSingleton_Should_Be_True_For_Single_Value()
    {
        var value = DateTimeOffset.Now;
        var period = new Period(value, value, true, true);

        period.IsSingleton.Should().BeTrue();
    }
}
