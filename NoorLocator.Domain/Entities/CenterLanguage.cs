using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class CenterLanguage : Entity
{
    public int CenterId { get; set; }

    public int LanguageId { get; set; }

    public Center? Center { get; set; }

    public Language? Language { get; set; }
}
