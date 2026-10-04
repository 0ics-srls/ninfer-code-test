using MyApp.Core.Enums;

namespace MyApp.Core.Entities;

public sealed record Todo : IEntity
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public TodoStatus Status { get; init; } = TodoStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateOnly? DueDate { get; init; }
}
