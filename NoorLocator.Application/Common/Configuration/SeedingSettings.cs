namespace NoorLocator.Application.Common.Configuration;

public class SeedingSettings
{
    public const string SectionName = "Seeding";

    public bool ApplyMigrations { get; set; } = true;

    public bool SeedReferenceData { get; set; } = true;

    public bool SeedDemoData { get; set; }
}
