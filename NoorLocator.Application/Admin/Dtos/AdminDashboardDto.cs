namespace NoorLocator.Application.Admin.Dtos;

public class AdminDashboardDto
{
    public int PendingCenterRequests { get; set; }

    public int PendingManagerRequests { get; set; }

    public int PendingCenterLanguageSuggestions { get; set; }

    public int PendingSuggestions { get; set; }

    public int TotalUsers { get; set; }

    public int TotalCenters { get; set; }

    public int TotalMajalis { get; set; }

    public int TotalAuditLogs { get; set; }
}
