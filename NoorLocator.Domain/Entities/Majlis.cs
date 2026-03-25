using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class Majlis : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public int CreatedByManagerId { get; set; }

    public Center? Center { get; set; }

    public User? CreatedByManager { get; set; }

    public ICollection<MajlisLanguage> MajlisLanguages { get; set; } = new List<MajlisLanguage>();
}
