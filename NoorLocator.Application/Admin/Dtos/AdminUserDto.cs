using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminUserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsEmailVerified { get; set; }

    public string PreferredLanguageCode { get; set; } = "en";

    public int AssignedCenterCount { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public bool CanDelete { get; set; }

    public string DeleteBlockedReason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
