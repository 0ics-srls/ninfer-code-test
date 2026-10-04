using System.ComponentModel.DataAnnotations;
using MyApp.Core.Entities;
using MyApp.Core.Enums;

namespace MyApp.Core.Dtos;

public sealed record TodoDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required TodoStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateOnly? DueDate { get; init; }
}

public static class TodoExtensions
{
    public static TodoDto ToDto(this Todo todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title,
        Status = todo.Status,
        CreatedAt = todo.CreatedAt,
        DueDate = todo.DueDate
    };

    public static Todo ToEntity(this TodoCreateDto dto) => new()
    {
        Id = 0,
        Title = dto.Title,
        Status = dto.Status,
        CreatedAt = DateTime.UtcNow,
        DueDate = dto.DueDate
    };
}

public sealed record TodoCreateDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    public TodoStatus Status { get; init; } = TodoStatus.Pending;
    public DateOnly? DueDate { get; init; }
}

public sealed record TodoUpdateDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    public required TodoStatus Status { get; init; }
    public DateOnly? DueDate { get; init; }
}

public sealed record TodoStatsDto
{
    public required int Total { get; init; }
    public required int Completed { get; init; }
    public required int Pending { get; init; }
}
