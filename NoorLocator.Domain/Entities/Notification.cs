using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class Notification : AuditableEntity
{
    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.System;

    public string RelatedEntityType { get; set; } = string.Empty;

    public int? RelatedEntityId { get; set; }

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public bool SentByEmail { get; set; }

    public DateTime? EmailSentAtUtc { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;

    public User? User { get; set; }
}
