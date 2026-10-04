using AwesomeAssertions;
using MyApp.Core.Dtos;
using MyApp.Core.Enums;
using Xunit;

namespace MyApp.Core.Tests.Dtos;

public class DtoTests
{
    [Fact]
    public void TodoDto_WithExpression_ProducesNewInstance()
    {
        var original = new TodoDto
        {
            Id = 1,
            Title = "Original",
            Status = TodoStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var modified = original with { Title = "Changed" };

        modified.Should().NotBeSameAs(original);
        modified.Title.Should().Be("Changed");
        modified.Id.Should().Be(original.Id);
        modified.Status.Should().Be(original.Status);
        modified.CreatedAt.Should().Be(original.CreatedAt);
    }
}
