using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class TodosStatsEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public TodosStatsEndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Stats_ReturnsDeltaAfterSeeding()
    {
        var before = await GetStatsAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"s1-a-{g}", "Pending");
        await SeedAsync($"s1-b-{g}", "Pending");
        await SeedAsync($"s1-c-{g}", "Completed");
        var after = await GetStatsAsync();

        (after.Total - before.Total).Should().Be(3);
        (after.Completed - before.Completed).Should().Be(1);
        (after.Pending - before.Pending).Should().Be(2);
    }

    [Fact]
    public async Task Get_Stats_ResponseShape_CamelCase()
    {
        var response = await _client.GetAsync("/api/Todos/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.EnumerateObject().Should().HaveCount(3);
        root.TryGetProperty("total", out var total).Should().BeTrue();
        root.TryGetProperty("completed", out var completed).Should().BeTrue();
        root.TryGetProperty("pending", out var pending).Should().BeTrue();
        total.ValueKind.Should().Be(JsonValueKind.Number);
        completed.ValueKind.Should().Be(JsonValueKind.Number);
        pending.ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public async Task Get_Stats_InProgressCountsAsPending()
    {
        var before = await GetStatsAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"s3-{g}", "InProgress");
        var after = await GetStatsAsync();

        (after.Pending - before.Pending).Should().Be(1);
        (after.Completed - before.Completed).Should().Be(0);
    }

    private async Task<StatsResponse> GetStatsAsync()
    {
        var response = await _client.GetAsync("/api/Todos/stats");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StatsResponse>())!;
    }

    private async Task SeedAsync(string title, string status)
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { Title = title, Status = status });
        response.EnsureSuccessStatusCode();
    }

    private sealed class StatsResponse
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
    }
}
