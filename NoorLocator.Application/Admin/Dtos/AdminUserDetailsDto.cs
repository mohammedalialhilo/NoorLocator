using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminUserDetailsDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsEmailVerified { get; set; }

    public string PreferredLanguageCode { get; set; } = "en";

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int AssignedCenterCount { get; set; }

    public bool CanDelete { get; set; }

    public string DeleteBlockedReason { get; set; } = string.Empty;

    public AdminUserNotificationPreferenceDto NotificationPreference { get; set; } = new();

    public IReadOnlyCollection<AdminManagedCenterDto> ManagedCenters { get; set; } = Array.Empty<AdminManagedCenterDto>();

    public IReadOnlyCollection<AdminManagedMajlisDto> CreatedMajalis { get; set; } = Array.Empty<AdminManagedMajlisDto>();

    public IReadOnlyCollection<AdminManagedAnnouncementDto> CreatedAnnouncements { get; set; } = Array.Empty<AdminManagedAnnouncementDto>();
}
