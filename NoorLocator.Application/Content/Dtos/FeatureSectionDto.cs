namespace NoorLocator.Application.Content.Dtos;

public class FeatureSectionDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyCollection<FeatureHighlightDto> Items { get; set; } = Array.Empty<FeatureHighlightDto>();
}
