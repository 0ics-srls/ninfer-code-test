using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using Scalar.AspNetCore;
using MyApp.Server.Extensions;
using MyApp.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddAppInfrastructure(builder.Configuration);
builder.Services.AddAppJson();

builder.Services.AddOptions<MyApp.Server.Options.AppOptions>()
    .Bind(builder.Configuration.GetSection("App"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("App:CorsOrigins").Get<string[]>() ?? [])
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddSingleton<GlobalExceptionMiddleware>();
builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, context, ct) =>
{
    var clrType = context.JsonTypeInfo.Type;
    if (Nullable.GetUnderlyingType(clrType) is { } underlying) clrType = underlying;
    if (!clrType.IsEnum) return Task.CompletedTask;

    schema.Type = JsonSchemaType.String;
    schema.Enum = [.. Enum.GetNames(clrType).Select(n => (JsonNode)n!)];
    return Task.CompletedTask;
}));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyApp.Infrastructure.Persistence.AppDbContext>();
    db.Database.Migrate();

    if (!db.Todos.Any())
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Todos.AddRange(
            new MyApp.Core.Entities.Todo { Id = 0, Title = "Learn Angular", Status = MyApp.Core.Enums.TodoStatus.Completed, DueDate = null },
            new MyApp.Core.Entities.Todo { Id = 0, Title = "Build my-app", Status = MyApp.Core.Enums.TodoStatus.InProgress, DueDate = today },
            new MyApp.Core.Entities.Todo { Id = 0, Title = "Plan week view", Status = MyApp.Core.Enums.TodoStatus.Pending, DueDate = today.AddDays(2) });
        db.SaveChanges();
    }
}

app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();
app.Run();

public partial class Program { }
