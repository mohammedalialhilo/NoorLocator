using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class UserCenterVisit : AuditableEntity
{
    public int UserId { get; set; }

    public int CenterId { get; set; }

    public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "page_view";

    public int VisitCount { get; set; } = 1;

    public User? User { get; set; }

    public Center? Center { get; set; }
}
