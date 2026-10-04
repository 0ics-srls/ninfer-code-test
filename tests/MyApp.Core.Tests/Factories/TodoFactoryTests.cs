using AwesomeAssertions;
using MyApp.Core.Entities;
using MyApp.Core.Enums;
using MyApp.Core.Tests.Factories;
using Xunit;

namespace MyApp.Core.Tests.Factories;

public class TodoFactoryTests
{
    [Fact]
    public void Create_ReturnsValidTodo()
    {
        var todo = TodoFactory.Create();

        todo.Title.Should().NotBeNullOrEmpty();
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCustomTitle_SetsProvidedTitle()
    {
        var todo = TodoFactory.Create(title: "My Custom Task");

        todo.Title.Should().Be("My Custom Task");
    }

    [Fact]
    public void Create_GeneratesUniqueTitles()
    {
        var todo1 = TodoFactory.Create();
        var todo2 = TodoFactory.Create();

        todo1.Title.Should().NotBe(todo2.Title);
    }
}
