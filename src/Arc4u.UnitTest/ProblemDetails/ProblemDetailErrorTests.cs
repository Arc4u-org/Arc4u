using Arc4u.Results;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Arc4u.UnitTest.ProblemDetail;

public class ProblemDetailErrorTests
{
    private readonly Fixture _fixture;

    public ProblemDetailErrorTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Minimal_Problem_Detail_Error_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        var error = sut is IError;
        error.Should().Be(true);
        var reason = sut is IReason;
        reason.Should().Be(true);
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_Type_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var uri = _fixture.Create<Uri>();
        // act
        var sut = ProblemDetailError.Create(detail).WithType(uri);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().Be(uri);
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_Instance_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var instance = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail).WithInstance(instance);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().Be(instance);
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_StatusCode_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail).WithStatusCode(StatusCodes.Status201Created);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().Be(StatusCodes.Status201Created);
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_Severity_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var severity = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail).WithSeverity(severity);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().Be(severity);
        sut.Title.Should().BeNull();
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_Title_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var title = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail).WithTitle(title);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().Be(title);
        sut.Metadata.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_One_Metadata_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var metaData = _fixture.Create<string>();
        // act
        var sut = ProblemDetailError.Create(detail).WithMetadata("key1", metaData);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().HaveCount(1);
        sut.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be(metaData);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_One_Metadata_One_Key_But_2_Values_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var metaData = _fixture.Create<string>();
        var metaData2 = _fixture.Create<string>();
        var key1 = "key1";
        // act
        var sut = ProblemDetailError.Create(detail).WithMetadata(key1, metaData).WithMetadata(key1, metaData2);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().HaveCount(1);
        sut.Metadata.Should().ContainSingle(key1, metaData);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Create_Problem_Detail_Error_With_One_Metadata_2_Keys_2_Values_Should()
    {
        // arrange
        var detail = _fixture.Create<string>();
        var metaData = _fixture.Create<string>();
        var metaData2 = _fixture.Create<string>();
        var key1 = "key1";
        var key2 = "key2";
        // act
        var sut = ProblemDetailError.Create(detail).WithMetadata(key1, metaData).WithMetadata(key2, metaData2);

        // assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ProblemDetailError>();
        sut.Message.Should().Be(detail);
        sut.Type.Should().BeNull();
        sut.Instance.Should().BeNull();
        sut.StatusCode.Should().BeNull();
        sut.Severity.Should().BeNull();
        sut.Title.Should().BeNull();
        sut.Metadata.Should().HaveCount(2);
        sut.Metadata.Should().ContainKeys(key1, key2);
        sut.Metadata[key1].Should().Be(metaData);
        sut.Metadata[key2].Should().Be(metaData2);
    }
}
