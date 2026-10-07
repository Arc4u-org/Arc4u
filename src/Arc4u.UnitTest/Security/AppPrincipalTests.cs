using Arc4u.Extensions;
using Arc4u.Security.Principal;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class AppPrincipalTests
{
    private readonly Fixture _fixture;

    public AppPrincipalTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    private static List<Operation> AllOperations => Enum.GetValues<Access>()
        .Select(o => new Operation { Name = o.GetValue(), ID = (int)o }).ToList();

    [Fact]
    public void Test_IsAuthorise_By_Enum_Should()
    {
        var authorization = GetAuthorization();

        var appAuthorization = new AppAuthorization(authorization);

        appAuthorization.IsAuthorized(Access.AccessApplication).Should().BeTrue();
    }

    [Fact]
    public void Test_IsAuthorise_By_Enum_Should_Not()
    {
        var authorization = GetAuthorization();

        var appAuthorization = new AppAuthorization(authorization);

        appAuthorization.IsAuthorized(Access.AccessApplication, Access.CanSeeSwaggerFacadeApi).Should().BeFalse();
    }

    [Fact]
    public void Test_IsAuthorise_By_Enum_Should_For_Specific()
    {
        var authorization = GetAuthorization();

        var appAuthorization = new AppAuthorization(authorization);

        appAuthorization.IsAuthorized("Specific", Access.AccessApplication, Access.CanSeeSwaggerFacadeApi).Should()
            .BeTrue();
    }

    [Fact]
    public void Test_IsAuthorise_By_Single_Name_Should()
    {
        var appAuthorization = new AppAuthorization(GetAuthorization());

        appAuthorization.IsAuthorized(Access.AccessApplication.GetValue()).Should().BeTrue();
        appAuthorization.IsAuthorized(Access.CanSeeSwaggerFacadeApi.GetValue()).Should().BeFalse();
    }

    [Fact]
    public void Test_IsAuthorise_By_Single_Name_On_Interface_Should()
    {
        IAuthorization appAuthorization = new AppAuthorization(GetAuthorization());

        appAuthorization.IsAuthorized(Access.AccessApplication.GetValue()).Should().BeTrue();
        appAuthorization.IsAuthorized(Access.CanSeeSwaggerFacadeApi.GetValue()).Should().BeFalse();
    }

    [Fact]
    public void Test_IsAuthorise_By_Single_Name_On_Principal_Should()
    {
        var principal = new AppPrincipal(GetAuthorization(), new System.Security.Claims.ClaimsIdentity(), "S-1-0-0");

        principal.IsAuthorized(Access.AccessApplication.GetValue()).Should().BeTrue();
        principal.IsAuthorized(Access.CanSeeSwaggerFacadeApi.GetValue()).Should().BeFalse();
    }

    private static Arc4u.Security.Principal.Authorization GetAuthorization()
    {
        var defaultScopedOperations = new ScopedOperations { Operations = [(int)Access.AccessApplication], Scope = "" };
        var specificScope = new ScopedOperations
        {
            Operations = [(int)Access.AccessApplication, (int)Access.CanSeeSwaggerFacadeApi], Scope = "Specific"
        };
        var authorization = new Arc4u.Security.Principal.Authorization
        {
            Operations = [defaultScopedOperations, specificScope],
            AllOperations = AllOperations,
            Roles = [new ScopedRoles { Roles = ["User"], Scope = "" }],
            Scopes = ["", "Specific"]
        };

        return authorization;
    }

    private enum Access
    {
        AccessApplication = 1,
        CanSeeSwaggerFacadeApi = 2
    }
}
