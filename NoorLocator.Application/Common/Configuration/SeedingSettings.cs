namespace NoorLocator.Application.Common.Configuration;

public class SeedingSettings
{
    public const string SectionName = "Seeding";

    public bool ApplyMigrations { get; set; } = true;

    public bool SeedReferenceData { get; set; } = true;

    public bool SeedAdminAccount { get; set; }

    public string AdminName { get; set; } = "NoorLocator Admin";

    public string AdminEmail { get; set; } = "admin@noorlocator.local";

    public string AdminPassword { get; set; } = string.Empty;

    public bool SeedDemoData { get; set; }
}
