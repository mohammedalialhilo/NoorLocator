namespace NoorLocator.Application.Content.Dtos;

public class AboutContentDto
{
    public string LanguageCode { get; set; } = "en";

    public string SiteTagline { get; set; } = string.Empty;

    public string Attribution { get; set; } = string.Empty;

    public NarrativeSectionDto HomeHero { get; set; } = new();

    public NarrativeSectionDto HomeMission { get; set; } = new();

    public FeatureSectionDto HomeFeatures { get; set; } = new();

    public NarrativeSectionDto Vision { get; set; } = new();

    public ListSectionDto ProblemStatement { get; set; } = new();

    public ListSectionDto Mission { get; set; } = new();

    public PrincipleSectionDto CorePrinciples { get; set; } = new();

    public NarrativeSectionDto WhoWeAre { get; set; } = new();

    public NarrativeSectionDto Closing { get; set; } = new();
}
