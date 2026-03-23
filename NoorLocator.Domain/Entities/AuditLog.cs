using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class AuditLog : AuditableEntity
{
    public int? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }

    public User? User { get; set; }
}
