using System.Reflection;
using Arc4u.Configuration;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Configuration;

[Trait("Category", "CI")]
public class ConfigurationHelperTests
{
    private const string Base = "Arc4u.UnitTest.Configuration.base.json";
    private const string Override = "Arc4u.UnitTest.Configuration.override.json";

    [Fact]
    public void GetConfigurationFromResourceStream_With_An_Assembly_Should_Load_The_Resources_In_Order()
    {
        var configuration = ConfigurationHelper.GetConfigurationFromResourceStream(typeof(ConfigurationHelperTests).Assembly, Base, Override);

        configuration["Feature:Enabled"].Should().Be("True");
        configuration["Feature:Limit"].Should().Be("2");
    }

    [Fact]
    public void GetConfigurationFromResourceStream_With_An_Assembly_Should_Ignore_A_Missing_Resource()
    {
        var configuration = ConfigurationHelper.GetConfigurationFromResourceStream(typeof(ConfigurationHelperTests).Assembly, "Missing.json", Base);

        configuration["Feature:Limit"].Should().Be("1");
    }

    [Fact]
    public void GetConfigurationFromResourceStream_With_An_Assembly_Should_Reject_Null()
    {
        FluentActions.Invoking(() => ConfigurationHelper.GetConfigurationFromResourceStream((Assembly)null!, Base))
                     .Should().Throw<ArgumentNullException>().WithParameterName("assembly");
    }

    [Fact]
    public void GetConfigurationFromResourceStream_With_Assembly_Names_Should_Give_The_Same_Result()
    {
        var assemblyName = typeof(ConfigurationHelperTests).Assembly.GetName().Name;

        var configuration = ConfigurationHelper.GetConfigurationFromResourceStream($"{assemblyName}, {Base}", $"{assemblyName}, {Override}");

        configuration["Feature:Enabled"].Should().Be("True");
        configuration["Feature:Limit"].Should().Be("2");
    }
}
