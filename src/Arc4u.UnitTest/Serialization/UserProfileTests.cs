using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using Arc4u.Security.Principal;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Serialization;

public class UserProfileTests
{
    private readonly Fixture _fixture;

    public UserProfileTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Json_Should()
    {
        var userProfile = new UserProfile(_fixture.Create<string>(),
            _fixture.Create<MailAddress>().Address,
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            new CultureInfo("fr-be"),
            _fixture.Create<string>(),
            _fixture.Create<string>());

        var json = JsonSerializer.Serialize(userProfile);

        var deserialized = JsonSerializer.Deserialize<UserProfile>(json);

        deserialized.Should().NotBeNull();
        deserialized.Domain.Should().Be(userProfile.Domain);
        deserialized.Email.Should().Be(userProfile.Email);
        deserialized.Company.Should().Be(userProfile.Company);
        deserialized.GivenName.Should().Be(userProfile.GivenName);
        deserialized.SurName.Should().Be(userProfile.SurName);
        deserialized.Sid.Should().Be(userProfile.Sid);
        deserialized.Mobile.Should().Be(userProfile.Mobile);
        deserialized.Telephone.Should().Be(userProfile.Telephone);
        deserialized.InternalPhone.Should().Be(userProfile.InternalPhone);
        deserialized.SamAccountName.Should().Be(userProfile.SamAccountName);
        deserialized.Culture.Should().Be(userProfile.Culture);
        deserialized.CurrentCulture.Should().Be(userProfile.CurrentCulture);
        deserialized.Email.Should().Be(userProfile.Email);
        deserialized.Department.Should().Be(userProfile.Department);
        deserialized.Description.Should().Be(userProfile.Description);
        deserialized.DisplayName.Should().Be(userProfile.DisplayName);
        deserialized.Fax.Should().Be(userProfile.Fax);
        deserialized.Initials.Should().Be(userProfile.Initials);
        deserialized.PostalCode.Should().Be(userProfile.PostalCode);
        deserialized.PrincipalName.Should().Be(userProfile.PrincipalName);
        deserialized.Room.Should().Be(userProfile.Room);
        deserialized.Sid.Should().Be(userProfile.Sid);
        deserialized.State.Should().Be(userProfile.State);
        deserialized.Street.Should().Be(userProfile.Street);
    }
}
