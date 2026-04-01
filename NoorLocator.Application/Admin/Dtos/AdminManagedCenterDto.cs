namespace NoorLocator.Application.Admin.Dtos;

public class AdminManagedCenterDto
{
    public int AssignmentId { get; set; }

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public string CenterCity { get; set; } = string.Empty;

    public string CenterCountry { get; set; } = string.Empty;

    public bool Approved { get; set; }
}
