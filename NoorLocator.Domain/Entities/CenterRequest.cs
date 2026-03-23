using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class CenterRequest : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string Description { get; set; } = string.Empty;

    public int RequestedByUserId { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;

    public User? RequestedByUser { get; set; }
}
