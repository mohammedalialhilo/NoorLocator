using System.ComponentModel.DataAnnotations;
using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Suggestions.Dtos;

public class CreateSuggestionDto
{
    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public SuggestionType Type { get; set; }
}
