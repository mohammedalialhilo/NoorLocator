using NoorLocator.Application.Languages.Dtos;

namespace NoorLocator.Application.Majalis.Dtos;

public class MajlisDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public string CenterCity { get; set; } = string.Empty;

    public string CenterCountry { get; set; } = string.Empty;

    public IReadOnlyCollection<LanguageDto> Languages { get; set; } = Array.Empty<LanguageDto>();
}
