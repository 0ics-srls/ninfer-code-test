using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MyApp.Server.Tests.Integration;

[Trait("Category", "Integration")]
public class OpenApiDocumentTests : IClassFixture<WebAppFactory>
{
    private static readonly string[] TodoStatusValues = ["Pending", "InProgress", "Completed"];

    private readonly HttpClient _client;

    public OpenApiDocumentTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<JsonNode> GetSchema(string name)
    {
        var doc = JsonNode.Parse(await _client.GetStringAsync("/openapi/v1.json"));
        return doc!["components"]!["schemas"]![name]!;
    }

    [Fact]
    public async Task OpenApiSpec_TodoStatusSchema_IsStringType_WithEnumValues()
    {
        var schema = await GetSchema("TodoStatus");

        schema["type"]!.GetValue<string>().Should().Be("string");
        var values = schema["enum"]!.AsArray().Select(e => e!.GetValue<string>()).ToArray();
        values.Should().BeEquivalentTo(TodoStatusValues);
    }

    [Fact]
    public async Task OpenApiSpec_TodoDto_Status_References_TodoStatus()
    {
        var statusRef = (await GetSchema("TodoDto"))["properties"]!["status"]!["$ref"]!;

        statusRef.GetValue<string>().Should().Be("#/components/schemas/TodoStatus");
    }

    [Fact]
    public async Task Week_DocumentedInOpenApi()
    {
        var doc = JsonNode.Parse(await _client.GetStringAsync("/openapi/v1.json"));

        doc!["paths"]!["/api/Todos/week"]!.Should().NotBeNull();
    }
}
