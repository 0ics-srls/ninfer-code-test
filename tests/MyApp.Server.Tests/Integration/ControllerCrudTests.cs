using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class ControllerCrudTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public ControllerCrudTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidBody_Returns201_TodoDto()
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { Title = "New Task", Status = "Pending" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("New Task");
        todo.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Post_WithEmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { Title = "", Status = "Pending" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WithValidBody_Returns200_UpdatedDto()
    {
        var createResp = await _client.PostAsJsonAsync("/api/Todos", new { Title = "Original", Status = "Pending" });
        var created = await createResp.Content.ReadFromJsonAsync<TodoResponse>();

        var updateResp = await _client.PutAsJsonAsync($"/api/Todos/{created!.Id}", new { Title = "Updated", Status = "Completed" });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<TodoResponse>();
        updated!.Title.Should().Be("Updated");
        updated.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Put_NonExistentId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/Todos/999", new { Title = "Nope", Status = "Pending" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingId_Returns204()
    {
        var createResp = await _client.PostAsJsonAsync("/api/Todos", new { Title = "To Delete", Status = "Pending" });
        var created = await createResp.Content.ReadFromJsonAsync<TodoResponse>();

        var deleteResp = await _client.DeleteAsync($"/api/Todos/{created!.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/Todos/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class TodoResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
