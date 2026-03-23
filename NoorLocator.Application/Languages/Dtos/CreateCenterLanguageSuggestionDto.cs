using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Languages.Dtos;

public class CreateCenterLanguageSuggestionDto
{
    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }

    [Range(1, int.MaxValue)]
    public int LanguageId { get; set; }
}
