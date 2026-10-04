using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyApp.Core.Errors;
using MyApp.Server.Middleware;
using Xunit;

namespace MyApp.Server.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static readonly ILogger<GlobalExceptionMiddleware> Logger =
        NullLogger<GlobalExceptionMiddleware>.Instance;

    private static async Task<(int statusCode, string? contentType, JsonDocument? body)> InvokeMiddleware(
        Func<Task> nextAction)
    {
        var middleware = new GlobalExceptionMiddleware(Logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _ => nextAction());

        context.Response.Body.Position = 0;
        JsonDocument? body = null;
        if (context.Response.Body.Length > 0)
        {
            body = await JsonDocument.ParseAsync(context.Response.Body);
        }

        return (context.Response.StatusCode, context.Response.ContentType, body);
    }

    [Fact]
    public async Task UnhandledException_Returns500_ProblemDetails()
    {
        var (status, _, body) = await InvokeMiddleware(() => throw new InvalidOperationException("kaboom"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().NotBeNull();
        body!.RootElement.GetProperty("status").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task UnhandledException_DetailDoesNotContainStackTrace()
    {
        var (_, _, body) = await InvokeMiddleware(() => throw new InvalidOperationException("secret internal detail"));

        var bodyJson = JsonSerializer.Serialize(body);
        bodyJson.Should().NotContain("at ");
        bodyJson.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task AppException_NotFound_Returns404()
    {
        var error = new AppError(AppErrorCode.NotFound, "Todo not found");
        var (status, _, body) = await InvokeMiddleware(() => throw new AppException(error));

        status.Should().Be(StatusCodes.Status404NotFound);
        body.Should().NotBeNull();
        body!.RootElement.GetProperty("detail").GetString().Should().Be("Todo not found");
    }

    [Fact]
    public async Task AppException_InvalidParams_Returns400()
    {
        var error = new AppError(AppErrorCode.InvalidParams, "Bad input");
        var (status, _, _) = await InvokeMiddleware(() => throw new AppException(error));

        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Response_ContentType_IsProblemJson()
    {
        var (_, contentType, _) = await InvokeMiddleware(() => throw new InvalidOperationException("test"));

        contentType.Should().Be("application/problem+json");
    }
}
