using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class Center : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string Description { get; set; } = string.Empty;

    public ICollection<CenterManager> CenterManagers { get; set; } = new List<CenterManager>();

    public ICollection<ManagerRequest> ManagerRequests { get; set; } = new List<ManagerRequest>();

    public ICollection<Majlis> Majalis { get; set; } = new List<Majlis>();

    public ICollection<EventAnnouncement> EventAnnouncements { get; set; } = new List<EventAnnouncement>();

    public ICollection<CenterImage> CenterImages { get; set; } = new List<CenterImage>();

    public ICollection<CenterLanguage> CenterLanguages { get; set; } = new List<CenterLanguage>();

    public ICollection<CenterLanguageSuggestion> CenterLanguageSuggestions { get; set; } = new List<CenterLanguageSuggestion>();

    public ICollection<UserCenterVisit> UserVisits { get; set; } = new List<UserCenterVisit>();

    public ICollection<UserCenterSubscription> UserSubscriptions { get; set; } = new List<UserCenterSubscription>();
}
