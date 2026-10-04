namespace MyApp.Core.Errors;

public sealed record AppError(
    AppErrorCode Code,
    string Message,
    IReadOnlyList<string>? Candidates = null,
    string? Suggestion = null);

public enum AppErrorCode
{
    NotFound,
    InvalidParams,
    Unauthorized,
    Timeout,
    NotSupported,
    InternalError
}

public sealed class AppException(AppError error) : Exception(error.Message)
{
    public AppError Error { get; } = error;
}
