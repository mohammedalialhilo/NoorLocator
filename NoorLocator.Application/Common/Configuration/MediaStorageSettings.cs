namespace NoorLocator.Application.Common.Configuration;

public class MediaStorageSettings
{
    public const string SectionName = "MediaStorage";

    public string Provider { get; set; } = "Local";

    public string PublicBasePath { get; set; } = "/uploads";

    public string RelativeRootPath { get; set; } = "uploads";

    public int MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024;
}
