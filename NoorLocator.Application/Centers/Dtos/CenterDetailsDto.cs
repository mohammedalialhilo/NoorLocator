using NoorLocator.Application.Majalis.Dtos;

namespace NoorLocator.Application.Centers.Dtos;

public class CenterDetailsDto : CenterSummaryDto
{
    public IReadOnlyCollection<MajlisDto> Majalis { get; set; } = Array.Empty<MajlisDto>();
}
