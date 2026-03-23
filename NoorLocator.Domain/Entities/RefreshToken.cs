using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class RefreshToken : AuditableEntity
{
    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public User? User { get; set; }
}
