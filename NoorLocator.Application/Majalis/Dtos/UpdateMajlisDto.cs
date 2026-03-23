using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Majalis.Dtos;

public class UpdateMajlisDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(50)]
    public string Time { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }

    public IReadOnlyCollection<int> LanguageIds { get; set; } = Array.Empty<int>();
}
