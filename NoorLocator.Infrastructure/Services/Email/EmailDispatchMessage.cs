namespace NoorLocator.Infrastructure.Services.Email;

public class EmailDispatchMessage
{
    public string ToEmail { get; init; } = string.Empty;

    public string ToName { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string HtmlBody { get; init; } = string.Empty;

    public string TextBody { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
