using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminCenterRequestDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string Description { get; set; } = string.Empty;

    public int RequestedByUserId { get; set; }

    public string RequestedByUserName { get; set; } = string.Empty;

    public string RequestedByUserEmail { get; set; } = string.Empty;

    public ModerationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
