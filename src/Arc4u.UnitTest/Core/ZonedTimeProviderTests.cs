using Arc4u.Configuration;
using Arc4u.Dependency;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arc4u.UnitTest.Core;

[Trait("Category", "CI")]
public class ZonedTimeProviderTests
{
    private static readonly TimeZoneInfo Brussels = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
    private static readonly TimeZoneInfo LosAngeles = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
    private static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static ZonedTimeProvider BrusselsAt(int year, int month, int day, int hour, int minute = 0)
        => new(Brussels, new FixedTimeProvider(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero)));

    [Theory]
    [InlineData(2023, 3, 26, 10, 29)]
    [InlineData(2026, 3, 29, 10, 25)]
    [InlineData(2030, 3, 31, 10, 27)]
    public void GetDaylightChanges_Brussels_ShouldReturnLastSundaysOfMarchAndOctober(int year, int startMonth, int startDay, int endMonth, int endDay)
    {
        var result = Brussels.GetDaylightChanges(year);

        result.Should().NotBeNull();
        result!.Start.Should().Be(new DateTime(year, startMonth, startDay, 2, 0, 0));
        result.End.Should().Be(new DateTime(year, endMonth, endDay, 3, 0, 0));
        result.Delta.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void GetDaylightChanges_LosAngeles_ShouldReturnUsRules()
    {
        var result = LosAngeles.GetDaylightChanges(2026);

        result.Should().NotBeNull();
        result!.Start.Should().Be(new DateTime(2026, 3, 8, 2, 0, 0));
        result.End.Should().Be(new DateTime(2026, 11, 1, 2, 0, 0));
        result.Delta.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void GetDaylightChanges_SouthernHemisphere_ShouldEndBeforeItStarts()
    {
        var result = Sydney.GetDaylightChanges(2026);

        result.Should().NotBeNull();
        result!.End.Should().Be(new DateTime(2026, 4, 5, 3, 0, 0));
        result.Start.Should().Be(new DateTime(2026, 10, 4, 2, 0, 0));
        result.Delta.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void GetDaylightChanges_NoDaylightSavingTime_ShouldReturnNull()
    {
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo").GetDaylightChanges(2026).Should().BeNull();
        TimeZoneInfo.Utc.GetDaylightChanges(2026).Should().BeNull();
    }

    [Fact]
    public void GetDaylightChanges_FixedDateRule_ShouldUseTheFixedDates()
    {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31),
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3));
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("TestZone", TimeSpan.Zero, "TestZone", "TestZone", "TestZone", [rule]);

        var result = timeZone.GetDaylightChanges(2023);

        result.Should().NotBeNull();
        result!.Start.Should().Be(new DateTime(2023, 3, 10, 2, 0, 0));
        result.End.Should().Be(new DateTime(2023, 11, 3, 2, 0, 0));
        result.Delta.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void GetTransitions_Brussels_ShouldReturnBothChangesAtOneUtc()
    {
        var transitions = Brussels.GetTransitions(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                  new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        transitions.Should().Equal(
            new TimeZoneTransition(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1), TimeSpan.FromHours(2), true),
            new TimeZoneTransition(new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(2), TimeSpan.FromHours(1), false));
    }

    [Fact]
    public void GetTransitions_EmptyPeriod_ShouldReturnNothing()
    {
        var instant = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Brussels.GetTransitions(instant, instant).Should().BeEmpty();
    }

    [Fact]
    public void ZonedTimeProvider_ShouldUseItsTimeZoneAndTheClockOfTheInnerProvider()
    {
        var sut = BrusselsAt(2026, 10, 29, 10);

        sut.LocalTimeZone.Should().BeSameAs(Brussels);
        sut.GetUtcNow().Should().Be(new DateTimeOffset(2026, 10, 29, 10, 0, 0, TimeSpan.Zero));
        sut.GetLocalNow().Should().Be(new DateTimeOffset(2026, 10, 29, 11, 0, 0, TimeSpan.FromHours(1)));
        sut.GetDaylightChanges(2026)!.End.Should().Be(new DateTime(2026, 10, 25, 3, 0, 0));
    }

    [Fact]
    public void AsLocalTime_Utc_ShouldConvertToTheTimeZoneOfTheProvider()
    {
        var result = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc).AsLocalTime(BrusselsAt(2026, 1, 1, 0));

        result.Should().Be(new DateTime(2026, 10, 29, 11, 0, 0));
        result.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void AsLocalTime_NotUtc_ShouldBeUnchanged()
    {
        var value = new DateTime(2026, 10, 29, 11, 0, 0, DateTimeKind.Unspecified);

        value.AsLocalTime(BrusselsAt(2026, 1, 1, 0)).Should().Be(value);
    }

    [Fact]
    public void AsUniversalTime_Unspecified_ShouldBeReadInTheTimeZoneOfTheProvider()
    {
        var result = new DateTime(2026, 10, 29, 11, 0, 0).AsUniversalTime(BrusselsAt(2026, 1, 1, 0));

        result.Should().Be(new DateTime(2026, 10, 29, 10, 0, 0));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AsUniversalTime_Local_ShouldUseTheTimeZoneOfTheMachine()
    {
        var value = new DateTime(2026, 10, 29, 11, 0, 0, DateTimeKind.Local);

        value.AsUniversalTime(BrusselsAt(2026, 1, 1, 0)).Should().Be(value.ToUniversalTime());
    }

    [Fact]
    public void AsUniversalTime_SkippedTime_ShouldMoveForwardByTheGap()
    {
        // 02:30 does not exist on 2026-03-29 in Brussels: it is read as 03:30 CEST.
        var result = new DateTime(2026, 3, 29, 2, 30, 0).AsUniversalTime(BrusselsAt(2026, 1, 1, 0));

        result.Should().Be(new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AsUniversalTime_RepeatedTime_ShouldBeTheFirstOccurrence()
    {
        // 02:30 happens twice on 2026-10-25 in Brussels: the first time is 02:30 CEST.
        var result = new DateTime(2026, 10, 25, 2, 30, 0).AsUniversalTime(BrusselsAt(2026, 1, 1, 0));

        result.Should().Be(new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SubtractWithDaylightTime_OverTheSpringChange_ShouldLastTwentyThreeHours()
    {
        var provider = BrusselsAt(2026, 1, 1, 0);

        new DateTime(2026, 3, 30).SubtractWithDaylightTime(new DateTime(2026, 3, 29), provider).Should().Be(TimeSpan.FromHours(23));
        new DateTime(2026, 10, 25).SubtractWithDaylightTime(new DateTime(2026, 10, 26), provider).Should().Be(TimeSpan.FromHours(25));
    }

    [Fact]
    public void ToLocalTime_DateTimeOffset_ShouldKeepTheInstant()
    {
        var value = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.FromHours(2));

        var result = value.ToLocalTime(BrusselsAt(2026, 1, 1, 0));

        result.Should().Be(value);
        result.Offset.Should().Be(TimeSpan.FromHours(1));
        result.Hour.Should().Be(11);
    }

    [Theory]
    [InlineData(17, 0, 6, 0)]   // 11:00 to 17:00 the same day
    [InlineData(11, 0, 0, 0)]   // now
    [InlineData(9, 0, 22, 0)]   // 09:00 the next day
    public void GetDueTimeOfDay_ShouldReturnTheTimeToTheNextOccurrence(int hour, int minute, int expectedHours, int expectedMinutes)
    {
        var sut = BrusselsAt(2026, 10, 29, 10); // 11:00 in Brussels

        sut.GetDueTimeOfDay(new TimeSpan(hour, minute, 0)).Should().Be(new TimeSpan(expectedHours, expectedMinutes, 0));
    }

    [Fact]
    public void GetDueTimeOfDay_OverTheSpringChange_ShouldCountTheShorterDay()
    {
        var sut = BrusselsAt(2026, 3, 28, 11); // 12:00 CET, the day before the change

        sut.GetDueTimeOfDay(new TimeSpan(11, 59, 0)).Should().Be(new TimeSpan(22, 59, 0));
        sut.GetDueTimeOfDay(new TimeSpan(2, 30, 0)).Should().Be(new TimeSpan(14, 30, 0)); // 02:30 is skipped: 03:30 CEST
    }

    [Fact]
    public void GetDueTimeOfDay_OverTheAutumnChange_ShouldCountTheLongerDay()
    {
        var sut = BrusselsAt(2026, 10, 24, 10); // 12:00 CEST, the day before the change

        sut.GetDueTimeOfDay(new TimeSpan(11, 59, 0)).Should().Be(new TimeSpan(24, 59, 0));
    }

    [Fact]
    public void GetDueTimeOfDay_OutOfRange_ShouldNotStartOrStartImmediately()
    {
        var sut = BrusselsAt(2026, 10, 29, 10);

        sut.GetDueTimeOfDay(TimeSpan.FromHours(25)).Should().Be(TimeSpan.FromMilliseconds(-1));
        sut.GetDueTimeOfDay(TimeSpan.FromHours(-1)).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AddZonedTimeProvider_ShouldUseTheTimeZoneOfTheApplicationConfig()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddILogger();
        services.AddSingleton(TimeProvider.System);
        services.AddApplicationConfig(config =>
        {
            config.ApplicationName = "TestApp";
            config.Environment.Name = "Development";
            config.Environment.LoggingName = "TestApp";
            config.Environment.TimeZone = "Europe/Brussels";
        });

        services.AddZonedTimeProvider();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetServices<TimeProvider>().Should().ContainSingle()
            .Which.Should().BeOfType<ZonedTimeProvider>()
            .Which.LocalTimeZone.HasSameRules(Brussels).Should().BeTrue();
    }

    [Fact]
    public void AddZonedTimeProvider_UnknownTimeZone_ShouldUseTheTimeZoneOfTheMachine()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddILogger();
        services.AddApplicationConfig(config =>
        {
            config.ApplicationName = "TestApp";
            config.Environment.Name = "Development";
            config.Environment.LoggingName = "TestApp";
            config.Environment.TimeZone = "Not/A_Zone";
        });

        services.AddZonedTimeProvider();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<TimeProvider>().LocalTimeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void AddZonedTimeProvider_Keyed_ShouldRegisterTheTimeZone()
    {
        var services = new ServiceCollection();

        services.AddZonedTimeProvider("NewYork", "America/New_York");
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredKeyedService<TimeProvider>("NewYork").LocalTimeZone.HasSameRules(TimeZoneInfo.FindSystemTimeZoneById("America/New_York")).Should().BeTrue();
    }

    [Fact]
    public void AddZonedTimeProvider_Keyed_UnknownTimeZone_ShouldThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddZonedTimeProvider("Nowhere", "Not/A_Zone");

        act.Should().Throw<TimeZoneNotFoundException>();
    }
}
