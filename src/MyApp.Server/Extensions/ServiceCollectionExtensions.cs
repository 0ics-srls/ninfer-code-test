using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;

namespace MyApp.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlite(configuration.GetConnectionString("Default")));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        return services;
    }

    public static IServiceCollection AddAppJson(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        return services;
    }
}
