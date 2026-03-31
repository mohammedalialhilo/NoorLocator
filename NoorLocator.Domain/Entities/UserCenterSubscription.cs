using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class UserCenterSubscription : AuditableEntity
{
    public int UserId { get; set; }

    public int CenterId { get; set; }

    public bool IsEmailNotificationsEnabled { get; set; } = true;

    public bool IsAppNotificationsEnabled { get; set; } = true;

    public DateTime? UpdatedAtUtc { get; set; }

    public User? User { get; set; }

    public Center? Center { get; set; }
}
