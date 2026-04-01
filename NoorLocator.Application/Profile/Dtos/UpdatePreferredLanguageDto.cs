using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Profile.Dtos;

public class UpdatePreferredLanguageDto
{
    [Required(ErrorMessage = "Preferred language is required.")]
    [StringLength(16, ErrorMessage = "Preferred language must be 16 characters or fewer.")]
    public string PreferredLanguageCode { get; set; } = string.Empty;
}
