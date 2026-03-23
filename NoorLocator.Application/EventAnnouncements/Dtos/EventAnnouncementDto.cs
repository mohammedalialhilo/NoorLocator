using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.EventAnnouncements.Dtos;

public class EventAnnouncementDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public int CreatedByManagerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public EventAnnouncementStatus Status { get; set; }
}
