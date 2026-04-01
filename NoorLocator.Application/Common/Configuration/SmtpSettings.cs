namespace NoorLocator.Application.Common.Configuration;

public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = "NoorLocator";

    public string Password { get; set; } = "rwaw fmtj fttf cpkj";

    public string FromEmail { get; set; } = "noorlocator@gmail.com";

    public string FromName { get; set; } = "NoorLocator";

    public bool UseSsl { get; set; } = true;

    public bool WriteToPickupDirectoryWhenDisabled { get; set; } = true;

    public string PickupDirectory { get; set; } = ".codex-temp/email-outbox";
}
