namespace NoorLocator.Application.Admin.Dtos;

public class AdminAuditLogDto
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
