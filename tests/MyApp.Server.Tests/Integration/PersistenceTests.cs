using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyApp.Core.Entities;
using MyApp.Core.Enums;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class PersistenceTests
{
    private static AppDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AppDbContext_CanCreateAndRetrieveTodo()
    {
        using var context = CreateContext();

        context.Todos.Add(new Todo { Id = 1, Title = "Test Todo", Status = TodoStatus.Pending });
        await context.SaveChangesAsync();

        var retrieved = await context.Todos.FirstOrDefaultAsync();
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Test Todo");
    }

    [Fact]
    public async Task Repository_GetAllAsync_ReturnsSeededEntities()
    {
        using var context = CreateContext();
        context.Todos.AddRange(
            new Todo { Id = 1, Title = "First" },
            new Todo { Id = 2, Title = "Second" });
        await context.SaveChangesAsync();
        var repo = new Repository<Todo>(context);

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Repository_GetByIdAsync_ReturnsEntity_WhenExists()
    {
        using var context = CreateContext();
        context.Todos.Add(new Todo { Id = 5, Title = "Found Me" });
        await context.SaveChangesAsync();
        var repo = new Repository<Todo>(context);

        var result = await repo.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Found Me");
    }

    [Fact]
    public async Task Repository_GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        using var context = CreateContext();
        var repo = new Repository<Todo>(context);

        var result = await repo.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Repository_AddAsync_PersistsEntity()
    {
        using var context = CreateContext();
        var repo = new Repository<Todo>(context);

        var todo = await repo.AddAsync(new Todo { Id = 0, Title = "New Todo", Status = TodoStatus.InProgress });

        todo.Id.Should().BeGreaterThan(0);
        var retrieved = await context.Todos.FindAsync(todo.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("New Todo");
    }

    [Fact]
    public async Task TodoConfiguration_Status_StoresAsString()
    {
        using var context = CreateContext();
        context.Todos.Add(new Todo { Id = 1, Title = "Enum Test", Status = TodoStatus.Completed });
        await context.SaveChangesAsync();

        var rawValue = await context.Database
            .SqlQueryRaw<string>("SELECT Status AS Value FROM Todos WHERE Id = 1")
            .FirstOrDefaultAsync();

        rawValue.Should().Be("Completed");
    }
}
