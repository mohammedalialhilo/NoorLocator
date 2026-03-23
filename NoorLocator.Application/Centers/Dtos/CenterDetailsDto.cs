using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;

namespace NoorLocator.Application.Centers.Dtos;

public class CenterDetailsDto : CenterSummaryDto
{
    public string Description { get; set; } = string.Empty;

    public IReadOnlyCollection<LanguageDto> Languages { get; set; } = Array.Empty<LanguageDto>();

    public IReadOnlyCollection<MajlisDto> Majalis { get; set; } = Array.Empty<MajlisDto>();
}
