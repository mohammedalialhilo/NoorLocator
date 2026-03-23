namespace NoorLocator.Application.Common.Models;

public class OperationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public static OperationResult Success(string message = "Request completed successfully.", int statusCode = 200)
        => new()
        {
            Succeeded = true,
            Message = message,
            StatusCode = statusCode
        };

    public static OperationResult Accepted(string message)
        => new()
        {
            Succeeded = true,
            Message = message,
            StatusCode = 202
        };

    public static OperationResult Failure(string message, int statusCode = 400)
        => new()
        {
            Succeeded = false,
            Message = message,
            StatusCode = statusCode
        };

    public static OperationResult NotImplemented(string message)
        => Failure(message, 501);
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; init; }

    public static OperationResult<T> Failure(string message, int statusCode = 400, T? data = default)
        => new()
        {
            Succeeded = false,
            Message = message,
            StatusCode = statusCode,
            Data = data
        };

    public new static OperationResult<T> NotImplemented(string message)
        => new()
        {
            Succeeded = false,
            Message = message,
            StatusCode = 501
        };

    public static OperationResult<T> Success(T? data, string message = "Request completed successfully.", int statusCode = 200)
        => new()
        {
            Succeeded = true,
            Message = message,
            StatusCode = statusCode,
            Data = data
        };
}
