using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using MyApp.Core.Dtos;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class TodosWeekEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public TodosWeekEndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Week_ReturnsSevenDaysWithMondayFirst()
    {
        await WipeAllAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = WeekPlanner.GetWeekStart(today);

        var response = await _client.GetAsync("/api/Todos/week");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var week = await response.Content.ReadFromJsonAsync<WeekResponse>();
        week!.Days.Should().HaveCount(7);
        week.Days[0].Date.Should().Be(monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        week.Days[6].Date.Should().Be(monday.AddDays(6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Week_BucketsDatedTodosByDay()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = WeekPlanner.GetWeekStart(today);
        var todayTitle = $"wk-today-{g}";
        var laterTitle = $"wk-plus2-{g}";
        await SeedAsync(todayTitle, today);
        await SeedAsync(laterTitle, today.AddDays(2));

        var response = await _client.GetAsync("/api/Todos/week");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var week = await response.Content.ReadFromJsonAsync<WeekResponse>();
        var todayIdx = ((int)today.DayOfWeek + 6) % 7;
        week!.Days[todayIdx].Todos.Select(t => t.Title).Should().ContainSingle().Which.Should().Be(todayTitle);

        var plus2InWeek = todayIdx + 2 <= 6;
        if (plus2InWeek)
        {
            week.Days[todayIdx + 2].Todos.Select(t => t.Title).Should().ContainSingle().Which.Should().Be(laterTitle);
        }
        else
        {
            week.Days.Sum(d => d.Todos.Count).Should().Be(1);
        }
        week.Days.Sum(d => d.Todos.Count).Should().Be(plus2InWeek ? 2 : 1);
        week.Undated.Should().BeEmpty();
    }

    [Fact]
    public async Task Week_PutsUndatedInUndatedBucket()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        var title = $"wk-undated-{g}";
        await SeedAsync(title, dueDate: null);

        var response = await _client.GetAsync("/api/Todos/week");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var week = await response.Content.ReadFromJsonAsync<WeekResponse>();
        week!.Undated.Select(t => t.Title).Should().ContainSingle().Which.Should().Be(title);
        week.Days.Sum(d => d.Todos.Count).Should().Be(0);
    }

    [Fact]
    public async Task Week_ExcludesOutOfWeekTodos()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        var title = $"wk-far-{g}";
        await SeedAsync(title, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(40));

        var response = await _client.GetAsync("/api/Todos/week");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var week = await response.Content.ReadFromJsonAsync<WeekResponse>();
        week!.Days.Sum(d => d.Todos.Count).Should().Be(0);
        week.Undated.Should().BeEmpty();
    }

    private async Task SeedAsync(string title, DateOnly? dueDate)
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { title, status = "Pending", dueDate = dueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        response.EnsureSuccessStatusCode();
    }

    private async Task WipeAllAsync()
    {
        var response = await _client.GetAsync("/api/Todos");
        response.EnsureSuccessStatusCode();
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        foreach (var item in load!.Data)
            await DeleteTodoAsync(item.Id);
    }

    private async Task DeleteTodoAsync(int id)
    {
        var response = await _client.DeleteAsync($"/api/Todos/{id}");
        response.EnsureSuccessStatusCode();
    }

    private sealed class LoadResponse
    {
        public List<TodoResponse> Data { get; set; } = [];
        public int? TotalCount { get; set; }
    }

    private sealed class WeekResponse
    {
        public List<WeekDay> Days { get; set; } = [];
        public List<TodoResponse> Undated { get; set; } = [];
    }

    private sealed class WeekDay
    {
        public string Date { get; set; } = string.Empty;
        public List<TodoResponse> Todos { get; set; } = [];
    }

    private sealed class TodoResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? DueDate { get; set; }
    }
}
