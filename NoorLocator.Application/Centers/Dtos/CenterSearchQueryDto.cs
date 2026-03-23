namespace NoorLocator.Application.Centers.Dtos;

public class CenterSearchQueryDto : CenterLocationQueryDto
{
    public string? Query { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? LanguageCode { get; set; }
}
