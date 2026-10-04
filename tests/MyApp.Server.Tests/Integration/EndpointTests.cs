using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class EndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public EndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVersion_Returns200_WithVersionString()
    {
        var response = await _client.GetAsync("/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VersionResponse>();
        content.Should().NotBeNull();
        content!.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task GetTodos_Returns200_WithList()
    {
        var response = await _client.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTodos_EmptyDb_ReturnsList_Not404()
    {
        var response = await _client.GetAsync("/api/todos");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiSpec_IsReachable_ContainsTodosPath()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("/api/Todos");
    }

    private sealed class VersionResponse
    {
        public string Version { get; set; } = string.Empty;
    }
}

public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Environment", "Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"DataSource=test-{Guid.NewGuid():N}.db"
            });
        });
    }
}
