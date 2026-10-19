using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Core;

[Trait("Category", "CI")]
public class FlagsEnumTests
{
    [Flags]
    private enum Days
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
        Weekend = Saturday | Sunday,
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(8, 3)]
    [InlineData(1L << 40, 40)]
    public void TryPowerOfTwoExponent_Should_Accept_Powers_Of_Two(long value, int expected)
    {
        FlagsEnum.TryPowerOfTwoExponent(value, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-2L)]
    [InlineData(3L)]
    [InlineData(6L)]
    [InlineData((1L << 24) + 1)]
    [InlineData((1L << 40) + 1)]
    public void TryPowerOfTwoExponent_Should_Reject_Non_Powers_Of_Two(long value)
    {
        FlagsEnum.TryPowerOfTwoExponent(value, out _).Should().BeFalse();
    }

    [Fact]
    public void TryPowerOfTwoExponent_Should_Reject_Null()
    {
        FlagsEnum.TryPowerOfTwoExponent(null, out _).Should().BeFalse();
    }

    [Fact]
    public void PowerOfTwoExponent_Should_Throw_For_Non_Power_Of_Two()
    {
        var act = () => FlagsEnum.PowerOfTwoExponent(3);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryPowerOfTwo_Should_Return_Matching_Value()
    {
        FlagsEnum.TryPowerOfTwo(3, out Days result).Should().BeTrue();
        result.Should().Be(Days.Wednesday);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-1.0)]
    [InlineData(10.0)]
    public void TryPowerOfTwo_Should_Reject_Non_Integral_Or_Undefined(double power)
    {
        FlagsEnum.TryPowerOfTwo(power, out Days _).Should().BeFalse();
    }

    [Fact]
    public void FlagValues_Should_Exclude_None_And_Composites()
    {
        FlagsEnum.FlagValues<Days>().Should().Equal(
            Days.Sunday, Days.Monday, Days.Tuesday, Days.Wednesday, Days.Thursday, Days.Friday, Days.Saturday);
    }

    [Fact]
    public void FlaggedValues_Should_Return_None_And_Composites()
    {
        FlagsEnum.FlaggedValues<Days>().Should().BeEquivalentTo([Days.None, Days.Weekend]);
    }

    [Fact]
    public void FlagValues_Of_Value_Should_Return_Individual_Flags()
    {
        FlagsEnum.FlagValues(Days.Weekend).Should().Equal(Days.Sunday, Days.Saturday);
    }

    [Fact]
    public void ContinuousFlagValues_Should_Wrap_Around_The_Week()
    {
        FlagsEnum.ContinuousFlagValues(Days.Friday | Days.Saturday | Days.Sunday | Days.Monday).Should().BeTrue();
        FlagsEnum.ContinuousFlagValues(Days.Monday | Days.Wednesday).Should().BeFalse();
    }
}
