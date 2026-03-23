using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class EventAnnouncement : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int CenterId { get; set; }

    public int CreatedByManagerId { get; set; }

    public EventAnnouncementStatus Status { get; set; } = EventAnnouncementStatus.Published;

    public Center? Center { get; set; }

    public User? CreatedByManager { get; set; }
}
