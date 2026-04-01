namespace NoorLocator.Application.Admin.Dtos;

public class AdminManagerAssignmentDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public string CenterCity { get; set; } = string.Empty;

    public string CenterCountry { get; set; } = string.Empty;

    public bool Approved { get; set; }

    public int MajlisCount { get; set; }

    public int AnnouncementCount { get; set; }
}
