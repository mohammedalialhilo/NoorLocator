namespace NoorLocator.Application.Content.Dtos;

public class ListSectionDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Items { get; set; } = Array.Empty<string>();
}
