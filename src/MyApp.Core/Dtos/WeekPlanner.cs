using MyApp.Core.Entities;

namespace MyApp.Core.Dtos;

public sealed record WeekDayDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<TodoDto> Todos { get; init; }
}

public sealed record WeekResponseDto
{
    public required IReadOnlyList<WeekDayDto> Days { get; init; }
    public required IReadOnlyList<TodoDto> Undated { get; init; }
}

public static class WeekPlanner
{
    public static DateOnly GetWeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static WeekResponseDto BuildWeek(DateOnly monday, IEnumerable<Todo> todos)
    {
        var all = todos.ToList();
        var days = Enumerable.Range(0, 7)
            .Select(offset => new WeekDayDto
            {
                Date = monday.AddDays(offset),
                Todos = all.Where(t => t.DueDate == monday.AddDays(offset)).OrderBy(t => t.Id).Select(t => t.ToDto()).ToList()
            }).ToList();
        return new WeekResponseDto
        {
            Days = days,
            Undated = all.Where(t => t.DueDate is null).OrderBy(t => t.Id).Select(t => t.ToDto()).ToList()
        };
    }
}
