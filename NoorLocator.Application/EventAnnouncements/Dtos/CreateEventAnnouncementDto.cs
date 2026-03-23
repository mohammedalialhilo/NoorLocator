using System.ComponentModel.DataAnnotations;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.EventAnnouncements.Dtos;

public class CreateEventAnnouncementDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }

    public EventAnnouncementStatus Status { get; set; } = EventAnnouncementStatus.Published;
}
