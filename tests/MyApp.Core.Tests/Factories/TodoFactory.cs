using MyApp.Core.Entities;
using MyApp.Core.Enums;

namespace MyApp.Core.Tests.Factories;

public static class TodoFactory
{
    private static int _counter;

    public static Todo Create(string? title = null, TodoStatus status = TodoStatus.Pending, DateOnly? dueDate = null) => new()
    {
        Id = System.Threading.Interlocked.Increment(ref _counter),
        Title = title ?? $"Todo {Guid.NewGuid():N}",
        Status = status,
        CreatedAt = DateTime.UtcNow,
        DueDate = dueDate
    };
}
