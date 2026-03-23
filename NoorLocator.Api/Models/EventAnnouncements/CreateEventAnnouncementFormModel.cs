using Microsoft.AspNetCore.Http;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Api.Models.EventAnnouncements;

public class CreateEventAnnouncementFormModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public EventAnnouncementStatus Status { get; set; } = EventAnnouncementStatus.Published;

    public IFormFile? Image { get; set; }
}
