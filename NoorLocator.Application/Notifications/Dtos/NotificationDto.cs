namespace NoorLocator.Application.Notifications.Dtos;

public class NotificationDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string RelatedEntityType { get; set; } = string.Empty;

    public int? RelatedEntityId { get; set; }

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool SentByEmail { get; set; }
}
