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
public class RepositoryCrudTests
{
    private static (AppDbContext, Repository<Todo>) Setup()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, new Repository<Todo>(context));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges_ToTitleAndStatus()
    {
        var (context, repo) = Setup();
        context.Todos.Add(new Todo { Id = 0, Title = "Original", Status = TodoStatus.Pending });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var id = context.Todos.AsNoTracking().First().Id;

        var existing = await repo.GetByIdAsync(id);
        var updated = existing! with { Title = "Changed", Status = TodoStatus.Completed };
        await repo.UpdateAsync(updated);

        var fromDb = await context.Todos.AsNoTracking().FirstAsync(t => t.Id == id);
        fromDb.Title.Should().Be("Changed");
        fromDb.Status.Should().Be(TodoStatus.Completed);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity_FromDatabase()
    {
        var (context, repo) = Setup();
        var todo = await repo.AddAsync(new Todo { Id = 0, Title = "To Delete" });
        var id = todo.Id;

        var result = await repo.DeleteAsync(id);

        result.Should().BeTrue();
        (await context.Todos.FindAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        var (_, repo) = Setup();

        var result = await repo.DeleteAsync(999);

        result.Should().BeFalse();
    }
}
