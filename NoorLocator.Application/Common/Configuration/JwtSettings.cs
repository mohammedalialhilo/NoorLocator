namespace NoorLocator.Application.Common.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "NoorLocator";

    public string Audience { get; set; } = "NoorLocator.Client";

    public string Key { get; set; } = "CHANGE-ME-TO-A-SECURE-32-CHARACTER-MINIMUM-SECRET";

    public int TokenLifetimeMinutes { get; set; } = 60;

    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
