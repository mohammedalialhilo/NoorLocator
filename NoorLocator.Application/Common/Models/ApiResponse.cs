namespace NoorLocator.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public static ApiResponse<T> SuccessResponse(T? data, string message = "Request completed successfully.")
        => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Failure(string message, T? data = default)
        => new()
        {
            Success = false,
            Message = message,
            Data = data
        };
}
