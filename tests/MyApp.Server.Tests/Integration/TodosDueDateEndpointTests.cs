using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class TodosDueDateEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public TodosDueDateEndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithDueDate_PersistsIsoDate()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");

        var response = await _client.PostAsJsonAsync("/api/Todos", new { title = $"e2e-due-{g}", status = "Pending", dueDate = "2026-09-09" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
        created!.DueDate.Should().Be("2026-09-09");

        var fetched = await (await _client.GetAsync($"/api/Todos/{created.Id}")).Content.ReadFromJsonAsync<TodoResponse>();
        fetched!.DueDate.Should().Be("2026-09-09");
    }

    [Fact]
    public async Task Create_WithoutDueDate_ReturnsNullField()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");

        var response = await _client.PostAsJsonAsync("/api/Todos", new { title = $"e2e-nodate-{g}", status = "Pending" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
        created!.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Update_CanSetDueDate()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        var created = await CreateAsync($"upd-set-{g}", dueDate: null);

        var put = await _client.PutAsJsonAsync($"/api/Todos/{created.Id}", new { title = created.Title, status = "Pending", dueDate = "2026-10-05" });

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await put.Content.ReadFromJsonAsync<TodoResponse>();
        updated!.DueDate.Should().Be("2026-10-05");

        var fetched = await (await _client.GetAsync($"/api/Todos/{created.Id}")).Content.ReadFromJsonAsync<TodoResponse>();
        fetched!.DueDate.Should().Be("2026-10-05");
    }

    [Fact]
    public async Task Update_CanClearDueDate()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        var created = await CreateAsync($"upd-clear-{g}", dueDate: "2026-10-05");

        var put = await _client.PutAsJsonAsync($"/api/Todos/{created.Id}", new { title = created.Title, status = "Pending", dueDate = (string?)null });

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await put.Content.ReadFromJsonAsync<TodoResponse>();
        updated!.DueDate.Should().BeNull();

        var fetched = await (await _client.GetAsync($"/api/Todos/{created.Id}")).Content.ReadFromJsonAsync<TodoResponse>();
        fetched!.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsDueDateInPayload()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await CreateAsync($"all-due-{g}", dueDate: "2026-09-09");

        var filter = Uri.EscapeDataString($"[[\"title\",\"contains\",\"{g}\"]]");
        var response = await _client.GetAsync($"/api/Todos?filter={filter}&take=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().ContainSingle().Which.DueDate.Should().Be("2026-09-09");
    }

    private async Task<TodoResponse> CreateAsync(string title, string? dueDate)
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { title, status = "Pending", dueDate });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();
        return created!;
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

    private sealed class TodoResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? DueDate { get; set; }
    }
}
