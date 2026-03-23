namespace NoorLocator.Application.Common.Configuration;

public class MediaStorageSettings
{
    public const string SectionName = "MediaStorage";

    public string PublicBasePath { get; set; } = "/uploads";

    public string RelativeRootPath { get; set; } = "..\\frontend\\uploads";

    public int MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024;
}
