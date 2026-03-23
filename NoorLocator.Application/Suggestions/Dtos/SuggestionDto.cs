using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Suggestions.Dtos;

public class SuggestionDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public SuggestionType Type { get; set; }

    public SuggestionReviewStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
