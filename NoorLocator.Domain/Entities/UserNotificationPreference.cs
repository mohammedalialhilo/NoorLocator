namespace NoorLocator.Domain.Entities;

public class UserNotificationPreference
{
    public int UserId { get; set; }

    public bool EmailNotificationsEnabled { get; set; } = true;

    public bool AppNotificationsEnabled { get; set; } = true;

    public bool MajlisNotificationsEnabled { get; set; } = true;

    public bool EventNotificationsEnabled { get; set; } = true;

    public bool CenterUpdatesEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public User? User { get; set; }
}
