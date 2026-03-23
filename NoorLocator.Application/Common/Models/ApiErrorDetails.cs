namespace NoorLocator.Application.Common.Models;

public sealed class ApiErrorDetails
{
    public string TraceId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
