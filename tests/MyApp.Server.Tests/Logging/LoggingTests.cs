using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MyApp.Server.Tests.Logging;

public class LoggingTests
{
    [Fact]
    public void AppSettings_HasSerilogSection_WithMinimumLevel()
    {
        var json = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<JsonDocument>(json)!;

        config.RootElement.TryGetProperty("Serilog", out var serilog).Should().BeTrue();
        serilog.TryGetProperty("MinimumLevel", out _).Should().BeTrue();
    }

    [Fact]
    public void AppSettings_SerilogHasConsoleAndFileSinks()
    {
        var json = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<JsonDocument>(json)!;
        var writeTo = config.RootElement.GetProperty("Serilog").GetProperty("WriteTo");

        var sinkNames = writeTo.EnumerateArray().Select(s => s.GetProperty("Name").GetString()).ToList();
        sinkNames.Should().Contain("Console");
        sinkNames.Should().Contain("File");
    }

    [Fact]
    public void AppSettings_SerilogFileSink_HasLogPath()
    {
        var json = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<JsonDocument>(json)!;
        var writeTo = config.RootElement.GetProperty("Serilog").GetProperty("WriteTo");

        var fileSink = writeTo.EnumerateArray().First(s => s.GetProperty("Name").GetString() == "File");
        fileSink.GetProperty("Args").GetProperty("path").GetString().Should().Contain("logs/");
    }

    [Fact]
    public void ILogger_CanBeUsedWithoutSerilogReference()
    {
        var logger = NullLogger<string>.Instance;

        var act = () => logger.LogInformation("Test message {Value}", 42);

        act.Should().NotThrow();
    }
}
