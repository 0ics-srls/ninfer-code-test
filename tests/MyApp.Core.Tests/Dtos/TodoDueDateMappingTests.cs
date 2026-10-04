using AwesomeAssertions;
using MyApp.Core.Dtos;
using MyApp.Core.Enums;
using MyApp.Core.Tests.Factories;
using Xunit;

namespace MyApp.Core.Tests.Dtos;

public class TodoDueDateMappingTests
{
    [Fact]
    public void ToEntity_WithDueDate_MapsToEntity()
    {
        var dto = new TodoCreateDto
        {
            Title = "Plan review",
            Status = TodoStatus.Pending,
            DueDate = new DateOnly(2026, 9, 7)
        };

        var entity = dto.ToEntity();

        entity.DueDate.Should().Be(new DateOnly(2026, 9, 7));
    }

    [Fact]
    public void ToEntity_WithoutDueDate_LeavesNull()
    {
        var dto = new TodoCreateDto
        {
            Title = "No date"
        };

        var entity = dto.ToEntity();

        entity.DueDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_RoundTripsDueDate()
    {
        var entity = TodoFactory.Create(dueDate: new DateOnly(2026, 9, 7));

        var dto = entity.ToDto();

        dto.DueDate.Should().Be(new DateOnly(2026, 9, 7));
    }

    [Fact]
    public void ToDto_NullDueDate_StaysNull()
    {
        var entity = TodoFactory.Create();

        var dto = entity.ToDto();

        dto.DueDate.Should().BeNull();
    }
}
