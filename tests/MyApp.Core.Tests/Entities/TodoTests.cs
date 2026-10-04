using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyApp.Core.Entities;
using MyApp.Core.Enums;
using Xunit;

namespace MyApp.Core.Tests.Entities;

public class TodoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var todo = new Todo
        {
            Id = 1,
            Title = "Learn Angular",
            Status = TodoStatus.InProgress,
            CreatedAt = new DateTime(2026, 1, 1)
        };

        todo.Id.Should().Be(1);
        todo.Title.Should().Be("Learn Angular");
        todo.Status.Should().Be(TodoStatus.InProgress);
        todo.CreatedAt.Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public void Create_DefaultStatus_IsPending()
    {
        var todo = new Todo { Id = 1, Title = "Test" };

        todo.Status.Should().Be(TodoStatus.Pending);
    }

    [Fact]
    public void TodoStatus_Pending_SerializesToString_NotNumber()
    {
        var json = JsonSerializer.Serialize(TodoStatus.Pending, JsonOptions);

        json.Should().Be("\"Pending\"");
    }

    [Fact]
    public void TodoStatus_AllValues_SerializeAsStrings()
    {
        JsonSerializer.Serialize(TodoStatus.Completed, JsonOptions).Should().Be("\"Completed\"");
        JsonSerializer.Serialize(TodoStatus.InProgress, JsonOptions).Should().Be("\"InProgress\"");
    }
}
