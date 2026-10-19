using Arc4u.Diagnostics;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Logging;

// The registry is shared by the whole process: each test uses its own values (from 1024) so the tests can run in parallel.
[Trait("Category", "CI")]
public class MessageCategoryRegistryTests
{
    [Fact]
    public void A_Registered_Name_Maps_To_Its_Value_And_Back()
    {
        MessageCategoryRegistry.Register("Security", (MessageCategory)1024);

        MessageCategoryRegistry.GetName((MessageCategory)1024).Should().Be("Security");
        MessageCategoryRegistry.TryGetCategory("security", out var category).Should().BeTrue();
        category.Should().Be((MessageCategory)1024);
    }

    [Fact]
    public void Registering_The_Same_Name_And_Value_Twice_Does_Nothing()
    {
        MessageCategoryRegistry.Register("Compliance", (MessageCategory)2048);

        var register = () => MessageCategoryRegistry.Register("Compliance", (MessageCategory)2048);

        register.Should().NotThrow();
    }

    [Fact]
    public void A_Name_Or_A_Value_Cannot_Be_Registered_Twice_With_Another_Value_Or_Name()
    {
        MessageCategoryRegistry.Register("Billing", (MessageCategory)4096);

        ((Action)(() => MessageCategoryRegistry.Register("Billing", (MessageCategory)8192))).Should().Throw<ArgumentException>();
        ((Action)(() => MessageCategoryRegistry.Register("Invoicing", (MessageCategory)4096))).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Business", 16384)]
    [InlineData("technical", 16384)]
    [InlineData("12", 16384)]
    [InlineData(" ", 16384)]
    [InlineData("Valid", 0)]
    [InlineData("Valid", -2)]
    [InlineData("Valid", 4)]
    public void Register_Rejects_Predefined_Numeric_Or_Invalid_Values(string name, short value)
    {
        var register = () => MessageCategoryRegistry.Register(name, (MessageCategory)value);

        register.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unregistered_Values_And_Names_Fall_Back_To_The_Number_And_To_False()
    {
        MessageCategoryRegistry.GetName(MessageCategory.Monitoring).Should().Be("Monitoring");
        MessageCategoryRegistry.GetName((MessageCategory)128).Should().Be("128");
        MessageCategoryRegistry.TryGetCategory("NeverRegistered", out _).Should().BeFalse();
        MessageCategoryRegistry.TryGetCategory("0", out _).Should().BeFalse();
    }
}
