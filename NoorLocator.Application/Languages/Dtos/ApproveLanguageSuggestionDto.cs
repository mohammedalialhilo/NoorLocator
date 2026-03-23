using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Languages.Dtos;

public class ApproveLanguageSuggestionDto
{
    [Range(1, int.MaxValue)]
    public int SuggestionId { get; set; }
}
