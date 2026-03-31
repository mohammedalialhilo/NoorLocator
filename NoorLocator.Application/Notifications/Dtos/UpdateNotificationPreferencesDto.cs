namespace NoorLocator.Application.Notifications.Dtos;

public class UpdateNotificationPreferencesDto
{
    public bool EmailNotificationsEnabled { get; set; }

    public bool AppNotificationsEnabled { get; set; }

    public bool MajlisNotificationsEnabled { get; set; }

    public bool EventNotificationsEnabled { get; set; }

    public bool CenterUpdatesEnabled { get; set; }
}
