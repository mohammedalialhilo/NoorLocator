namespace NoorLocator.Application.Content.Dtos;

public class PrincipleSectionDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyCollection<PrincipleDto> Items { get; set; } = Array.Empty<PrincipleDto>();
}
