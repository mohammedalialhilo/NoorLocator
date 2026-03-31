namespace NoorLocator.Application.Centers.Dtos;

public class CenterSubscriptionDto
{
    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public bool IsEmailNotificationsEnabled { get; set; }

    public bool IsAppNotificationsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
}
