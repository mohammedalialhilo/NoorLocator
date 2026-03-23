using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class CenterImage : AuditableEntity
{
    public int CenterId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int UploadedByManagerId { get; set; }

    public bool IsPrimary { get; set; }

    public Center? Center { get; set; }

    public User? UploadedByManager { get; set; }
}
