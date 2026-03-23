using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class CenterLanguageSuggestion : Entity
{
    public int CenterId { get; set; }

    public int LanguageId { get; set; }

    public int SuggestedByUserId { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;

    public Center? Center { get; set; }

    public Language? Language { get; set; }

    public User? SuggestedByUser { get; set; }
}
