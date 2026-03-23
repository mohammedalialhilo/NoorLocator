using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminCenterLanguageSuggestionDto
{
    public int Id { get; set; }

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public int SuggestedByUserId { get; set; }

    public string SuggestedByUserName { get; set; } = string.Empty;

    public string SuggestedByUserEmail { get; set; } = string.Empty;

    public ModerationStatus Status { get; set; }
}
