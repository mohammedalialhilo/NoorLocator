namespace NoorLocator.Application.Common.Configuration;

public class FrontendSettings
{
    public const string SectionName = "Frontend";

    public string RelativeRootPath { get; set; } = "frontend";

    public string ApiBaseUrl { get; set; } = string.Empty;

    public string PublicOrigin { get; set; } = string.Empty;
}
