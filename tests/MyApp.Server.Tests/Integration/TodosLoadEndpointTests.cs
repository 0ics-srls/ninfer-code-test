using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class TodosLoadEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public TodosLoadEndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_NoOptions_ReturnsAllInData_TotalCountNull()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"w1-a-{g}");
        await SeedAsync($"w1-b-{g}");
        await SeedAsync($"w1-c-{g}");

        var response = await _client.GetAsync("/api/Todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load.Should().NotBeNull();
        var titles = load!.Data.Select(t => t.Title).ToList();
        titles.Should().Contain($"w1-a-{g}");
        titles.Should().Contain($"w1-b-{g}");
        titles.Should().Contain($"w1-c-{g}");
        // Library sentinel: totalCount = -1 when requireTotalCount is not requested.
        load.TotalCount.Should().Be(-1);
    }

    [Fact]
    public async Task Get_RequireTotalCount_ReturnsTotalCount()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"w2-a-{g}");
        await SeedAsync($"w2-b-{g}");
        await SeedAsync($"w2-c-{g}");

        var response = await _client.GetAsync("/api/Todos?requireTotalCount=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Get_SkipTake_ReturnsPage()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"A-{g}");
        await SeedAsync($"B-{g}");
        await SeedAsync($"C-{g}");

        var sort = Uri.EscapeDataString("[{\"selector\":\"title\",\"desc\":false}]");
        var response = await _client.GetAsync($"/api/Todos?requireTotalCount=true&sort={sort}&skip=1&take=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().ContainSingle().Which.Title.Should().Be($"B-{g}");
    }

    [Fact]
    public async Task Get_SortDesc_ReversesOrder()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"A-{g}");
        await SeedAsync($"B-{g}");

        var sort = Uri.EscapeDataString("[{\"selector\":\"title\",\"desc\":true}]");
        var response = await _client.GetAsync($"/api/Todos?sort={sort}&take=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().HaveCount(2);
        load.Data[0].Title.Should().Be($"B-{g}");
    }

    [Fact]
    public async Task Get_FilterTitleContains_IsCaseInsensitive()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"Alpha-xyz-{g}");
        await SeedAsync($"Beta-{g}");

        var filter = Uri.EscapeDataString("[\"title\",\"contains\",\"XYZ\"]");
        var response = await _client.GetAsync($"/api/Todos?filter={filter}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().ContainSingle().Which.Title.Should().Be($"Alpha-xyz-{g}");
    }

    [Fact]
    public async Task Get_FilterStatusEq_FiltersByEnumString()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"p6-{g}", "Pending");
        await SeedAsync($"z6-{g}", "Completed");

        var filter = Uri.EscapeDataString("[\"status\",\"=\",\"Pending\"]");
        var response = await _client.GetAsync($"/api/Todos?filter={filter}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().ContainSingle().Which.Title.Should().Be($"p6-{g}");
    }

    [Fact]
    public async Task Get_FilterCreatedAtRange_UsesIsoDates()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"c7-{g}");

        var wide = Uri.EscapeDataString("[[\"createdAt\",\">=\",\"2000-01-01T00:00:00Z\"],\"and\",[\"createdAt\",\"<=\",\"2999-01-01T00:00:00Z\"]]");
        var wideResp = await _client.GetAsync($"/api/Todos?filter={wide}");
        var wideLoad = await wideResp.Content.ReadFromJsonAsync<LoadResponse>();
        wideResp.StatusCode.Should().Be(HttpStatusCode.OK);
        wideLoad!.Data.Select(t => t.Title).Should().Contain($"c7-{g}");

        var narrow = Uri.EscapeDataString("[[\"createdAt\",\">=\",\"2000-01-01T00:00:00Z\"],\"and\",[\"createdAt\",\"<=\",\"2001-01-01T00:00:00Z\"]]");
        var narrowResp = await _client.GetAsync($"/api/Todos?filter={narrow}");
        var narrowLoad = await narrowResp.Content.ReadFromJsonAsync<LoadResponse>();
        narrowResp.StatusCode.Should().Be(HttpStatusCode.OK);
        narrowLoad!.Data.Select(t => t.Title).Should().NotContain($"c7-{g}");
    }

    [Fact]
    public async Task Get_Combined_FilterSortPage()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"todo-a-{g}");
        await SeedAsync($"todo-a-{g}");
        await SeedAsync($"todo-b-{g}", "Completed");
        await SeedAsync("unrelated-row");

        var filter = Uri.EscapeDataString($"[[\"title\",\"contains\",\"{g}\"]]");
        var sort = Uri.EscapeDataString("[{\"selector\":\"title\",\"desc\":false}]");
        var response = await _client.GetAsync($"/api/Todos?filter={filter}&sort={sort}&skip=0&take=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().HaveCount(2);
        load.Data[0].Title.Should().Be($"todo-a-{g}");
    }

    [Fact]
    public async Task Get_SkipBeyondEnd_ReturnsEmptyData()
    {
        await WipeAllAsync();
        var g = Guid.NewGuid().ToString("N");
        await SeedAsync($"w9-a-{g}");
        await SeedAsync($"w9-b-{g}");

        var response = await _client.GetAsync("/api/Todos?requireTotalCount=true&skip=1000&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        load!.Data.Should().BeEmpty();
        load.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private async Task WipeAllAsync()
    {
        var response = await _client.GetAsync("/api/Todos");
        response.EnsureSuccessStatusCode();
        var load = await response.Content.ReadFromJsonAsync<LoadResponse>();
        foreach (var item in load!.Data)
            await DeleteTodoAsync(item.Id);
    }

    private async Task SeedAsync(string title, string status = "Pending")
    {
        var response = await _client.PostAsJsonAsync("/api/Todos", new { Title = title, Status = status });
        response.EnsureSuccessStatusCode();
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
    }
}
