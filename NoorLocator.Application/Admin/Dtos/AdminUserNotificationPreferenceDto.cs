namespace NoorLocator.Application.Admin.Dtos;

public class AdminUserNotificationPreferenceDto
{
    public bool EmailNotificationsEnabled { get; set; }

    public bool AppNotificationsEnabled { get; set; }

    public bool MajlisNotificationsEnabled { get; set; }

    public bool EventNotificationsEnabled { get; set; }

    public bool CenterUpdatesEnabled { get; set; }
}
