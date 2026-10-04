using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace MyApp.Server.Tests.Options;

public class AppConfigTests
{
    [Fact]
    public void AppSettings_Contains_App_Section_With_CorsOrigins()
    {
        var json = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<JsonDocument>(json)!;

        config.RootElement.TryGetProperty("App", out var appSection).Should().BeTrue();
        appSection.TryGetProperty("CorsOrigins", out _).Should().BeTrue();
    }

    [Fact]
    public void AppSettings_Contains_ConnectionStrings_Default()
    {
        var json = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<JsonDocument>(json)!;

        config.RootElement.TryGetProperty("ConnectionStrings", out var cs).Should().BeTrue();
        cs.TryGetProperty("Default", out _).Should().BeTrue();
    }
}
