using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using MyApp.Core.Dtos;
using MyApp.Core.Enums;
using Xunit;

namespace MyApp.Core.Tests.Dtos;

public class TodoCreateDtoTests
{
    [Fact]
    public void Create_WithValidTitle_Succeeds()
    {
        var dto = new TodoCreateDto { Title = "My Task" };

        dto.Title.Should().Be("My Task");
        dto.Status.Should().Be(TodoStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyTitle_FailsValidation()
    {
        var dto = new TodoCreateDto { Title = "" };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(TodoCreateDto.Title)));
    }

    [Fact]
    public void UpdateDto_HasTitleAndStatus()
    {
        var dto = new TodoUpdateDto { Title = "Updated", Status = TodoStatus.Completed };

        dto.Title.Should().Be("Updated");
        dto.Status.Should().Be(TodoStatus.Completed);
    }
}
