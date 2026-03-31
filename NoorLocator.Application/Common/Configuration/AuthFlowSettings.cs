namespace NoorLocator.Application.Common.Configuration;

public class AuthFlowSettings
{
    public const string SectionName = "AuthFlow";

    public int EmailVerificationTokenLifetimeMinutes { get; set; } = 1440;

    public int PasswordResetTokenLifetimeMinutes { get; set; } = 60;

    public string VerifyEmailPath { get; set; } = "verify-email.html";

    public string ResetPasswordPath { get; set; } = "reset-password.html";
}
