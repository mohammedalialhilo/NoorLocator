namespace NoorLocator.Application.Admin.Dtos;

public class AdminManagedAnnouncementDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public string CenterCity { get; set; } = string.Empty;

    public string CenterCountry { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
