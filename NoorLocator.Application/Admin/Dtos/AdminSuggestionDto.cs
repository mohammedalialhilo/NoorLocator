using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminSuggestionDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public SuggestionType Type { get; set; }

    public SuggestionReviewStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
