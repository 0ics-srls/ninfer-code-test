using AwesomeAssertions;
using MyApp.Core.Errors;
using Xunit;

namespace MyApp.Core.Tests.Errors;

public class ErrorTests
{
    [Fact]
    public void AppError_Stores_CodeAndMessage()
    {
        var error = new AppError(AppErrorCode.NotFound, "Item not found");

        error.Code.Should().Be(AppErrorCode.NotFound);
        error.Message.Should().Be("Item not found");
    }

    [Fact]
    public void AppError_Stores_OptionalCandidatesAndSuggestion()
    {
        var error = new AppError(
            AppErrorCode.InvalidParams,
            "Invalid value",
            ["A", "B"],
            "Try one of the candidates");

        error.Candidates.Should().NotBeNull();
        error.Candidates.Should().HaveCount(2);
        error.Suggestion.Should().Be("Try one of the candidates");
    }

    [Theory]
    [InlineData(AppErrorCode.NotFound)]
    [InlineData(AppErrorCode.InvalidParams)]
    [InlineData(AppErrorCode.Unauthorized)]
    [InlineData(AppErrorCode.Timeout)]
    [InlineData(AppErrorCode.NotSupported)]
    [InlineData(AppErrorCode.InternalError)]
    public void AppErrorCode_HasAllExpectedValues(AppErrorCode code)
    {
        ((int)code).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void AppException_CarriesError()
    {
        var error = new AppError(AppErrorCode.NotFound, "Missing");

        var exception = new AppException(error);

        exception.Error.Should().Be(error);
        exception.Message.Should().Be("Missing");
    }
}
