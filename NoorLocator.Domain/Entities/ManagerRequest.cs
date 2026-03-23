using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class ManagerRequest : AuditableEntity
{
    public int UserId { get; set; }

    public int CenterId { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;

    public User? User { get; set; }

    public Center? Center { get; set; }
}
