using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class MajlisLanguage : Entity
{
    public int MajlisId { get; set; }

    public int LanguageId { get; set; }

    public Majlis? Majlis { get; set; }

    public Language? Language { get; set; }
}
