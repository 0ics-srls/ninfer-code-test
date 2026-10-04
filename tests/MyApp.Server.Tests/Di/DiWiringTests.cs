using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Core.Entities;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;
using MyApp.Server.Extensions;
using MyApp.Server.Options;
using Xunit;

namespace MyApp.Server.Tests.Di;

[Trait("Category", "Integration")]
public class DiWiringTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "DataSource=:memory:",
                ["App:CorsOrigins:0"] = "http://localhost:4200"
            })
            .Build();
        services.AddAppInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Resolves_IRepository_Todo()
    {
        var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var repo = scope.ServiceProvider.GetService<IRepository<Todo>>();

        repo.Should().NotBeNull();
    }

    [Fact]
    public void Resolves_AppDbContext()
    {
        var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetService<AppDbContext>();

        db.Should().NotBeNull();
    }

    [Fact]
    public void Resolves_IOptions_AppOptions_BindsCorsOrigins()
    {
        var services = new ServiceCollection();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:CorsOrigins:0"] = "http://localhost:4200",
                ["App:CorsOrigins:1"] = "http://localhost:4300"
            })
            .Build();
        services.AddOptions<AppOptions>().Bind(config.GetSection("App"));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        options.Value.CorsOrigins.Should().Contain(["http://localhost:4200", "http://localhost:4300"]);
    }

    [Fact]
    public void AddAppJson_Registers_Controllers_WithEnumStringConverter()
    {
        var services = new ServiceCollection();
        services.AddAppJson();
        var provider = services.BuildServiceProvider();

        var mvcOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>();
        var converters = mvcOptions.Value.JsonSerializerOptions.Converters;

        converters.OfType<System.Text.Json.Serialization.JsonStringEnumConverter>().Should().HaveCount(1);
    }
}
