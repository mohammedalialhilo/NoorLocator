using NoorLocator.Domain.Common;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Domain.Entities;

public class Suggestion : AuditableEntity
{
    public int UserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public SuggestionType Type { get; set; } = SuggestionType.Feature;

    public SuggestionReviewStatus Status { get; set; } = SuggestionReviewStatus.Pending;

    public User? User { get; set; }
}
