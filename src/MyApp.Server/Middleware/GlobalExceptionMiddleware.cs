using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyApp.Core.Errors;

namespace MyApp.Server.Middleware;

#pragma warning disable CA1848 // LoggerMessage delegates — scaffold uses direct logging

internal sealed class GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "App error: {Code}", ex.Error.Code);
            await WriteProblem(context, MapStatus(ex.Error.Code), ex.Error.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "An internal error occurred.");
        }
    }

    private static int MapStatus(AppErrorCode code) => code switch
    {
        AppErrorCode.NotFound => StatusCodes.Status404NotFound,
        AppErrorCode.InvalidParams => StatusCodes.Status400BadRequest,
        AppErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };

    private static async Task WriteProblem(HttpContext ctx, int status, string detail)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status >= 500 ? "Server Error" : "Client Error",
            Detail = detail
        };
        var json = System.Text.Json.JsonSerializer.Serialize(problem);
        await ctx.Response.WriteAsync(json);
    }
}
