using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<CenterRequest> CenterRequests { get; set; } = new List<CenterRequest>();

    public ICollection<CenterManager> ManagedCenters { get; set; } = new List<CenterManager>();

    public ICollection<ManagerRequest> ManagerRequests { get; set; } = new List<ManagerRequest>();

    public ICollection<Majlis> CreatedMajalis { get; set; } = new List<Majlis>();

    public ICollection<EventAnnouncement> EventAnnouncements { get; set; } = new List<EventAnnouncement>();

    public ICollection<CenterImage> UploadedCenterImages { get; set; } = new List<CenterImage>();

    public ICollection<CenterLanguageSuggestion> CenterLanguageSuggestions { get; set; } = new List<CenterLanguageSuggestion>();

    public ICollection<Suggestion> Suggestions { get; set; } = new List<Suggestion>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
