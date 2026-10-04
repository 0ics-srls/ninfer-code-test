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
public class RepositoryQueryTests
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
    public void Query_ReturnsIQueryable_Composable()
    {
        var (_, repo) = Setup();

        var query = repo.Query();

        query.Should().BeAssignableTo<IQueryable<Todo>>();
    }

    [Fact]
    public async Task Query_WhereOrderBySkipTake_ReturnsExpectedPage()
    {
        var (_, repo) = Setup();
        await repo.AddAsync(new Todo { Id = 0, Title = "B-x", Status = TodoStatus.Pending });
        await repo.AddAsync(new Todo { Id = 0, Title = "A-x", Status = TodoStatus.Pending });
        await repo.AddAsync(new Todo { Id = 0, Title = "C-x", Status = TodoStatus.Pending });

        var page = await repo.Query()
            .Where(t => t.Title.Contains('x'))
            .OrderBy(t => t.Title)
            .Skip(1)
            .Take(1)
            .ToListAsync();

        page.Should().ContainSingle().Which.Title.Should().Be("B-x");
    }

    [Fact]
    public async Task Query_DoesNotTrack_Entities()
    {
        var (context, repo) = Setup();
        await repo.AddAsync(new Todo { Id = 0, Title = "NoTrack", Status = TodoStatus.Pending });
        context.ChangeTracker.Clear();

        await repo.Query().ToListAsync();

        context.ChangeTracker.Entries<Todo>().Should().BeEmpty();
    }

    [Fact]
    public async Task Query_OnEmptySet_ReturnsEmptyQueryable()
    {
        var (_, repo) = Setup();

        var results = await repo.Query().ToListAsync();

        results.Should().BeEmpty();
    }
}
