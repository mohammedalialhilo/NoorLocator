using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Services.Email;

public class NoorLocatorEmailService(
    IEmailDeliveryService emailDeliveryService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthFlowSettings> authFlowOptions,
    IOptions<FrontendSettings> frontendOptions,
    IOptions<SmtpSettings> smtpOptions) : INoorLocatorEmailService
{
    private readonly AuthFlowSettings authFlowSettings = authFlowOptions.Value;
    private readonly FrontendSettings frontendSettings = frontendOptions.Value;
    private readonly SmtpSettings smtpSettings = smtpOptions.Value;

    public Task SendVerificationEmailAsync(User user, string token, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl(authFlowSettings.VerifyEmailPath, new Dictionary<string, string?>
        {
            ["token"] = token,
            ["email"] = user.Email
        });

        return SendAsync(
            user,
            "Verify your NoorLocator email",
            $"""
            <p>Assalamu alaikum {Escape(user.Name)},</p>
            <p>Please confirm that you own this email address so NoorLocator can unlock your account.</p>
            <p><a href="{Escape(url)}">Verify my email</a></p>
            <p>This link expires in {authFlowSettings.EmailVerificationTokenLifetimeMinutes} minutes.</p>
            """,
            $"""
            Assalamu alaikum {user.Name},

            Please confirm that you own this email address so NoorLocator can unlock your account.
            Verify your email: {url}

            This link expires in {authFlowSettings.EmailVerificationTokenLifetimeMinutes} minutes.
            """,
            cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl(authFlowSettings.ResetPasswordPath, new Dictionary<string, string?>
        {
            ["token"] = token,
            ["email"] = user.Email
        });

        return SendAsync(
            user,
            "Reset your NoorLocator password",
            $"""
            <p>Assalamu alaikum {Escape(user.Name)},</p>
            <p>We received a request to reset your NoorLocator password.</p>
            <p><a href="{Escape(url)}">Choose a new password</a></p>
            <p>This link expires in {authFlowSettings.PasswordResetTokenLifetimeMinutes} minutes and can only be used once.</p>
            """,
            $"""
            Assalamu alaikum {user.Name},

            We received a request to reset your NoorLocator password.
            Choose a new password: {url}

            This link expires in {authFlowSettings.PasswordResetTokenLifetimeMinutes} minutes and can only be used once.
            """,
            cancellationToken);
    }

    public Task SendPasswordChangedConfirmationAsync(User user, CancellationToken cancellationToken = default)
    {
        return SendAsync(
            user,
            "Your NoorLocator password was changed",
            $"""
            <p>Assalamu alaikum {Escape(user.Name)},</p>
            <p>Your NoorLocator password was changed successfully.</p>
            <p>If you did not make this change, please reset your password again immediately.</p>
            """,
            $"""
            Assalamu alaikum {user.Name},

            Your NoorLocator password was changed successfully.
            If you did not make this change, please reset your password again immediately.
            """,
            cancellationToken);
    }

    public Task SendMajlisNotificationAsync(User user, Center center, Majlis majlis, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl("center-details.html", new Dictionary<string, string?>
        {
            ["id"] = center.Id.ToString()
        });

        return SendAsync(
            user,
            $"New majlis at {center.Name}",
            $"""
            <p>Assalamu alaikum {Escape(user.Name)},</p>
            <p>{Escape(center.Name)} has published a new majlis: <strong>{Escape(majlis.Title)}</strong>.</p>
            <p>{Escape(majlis.Description)}</p>
            <p>Date: {Escape(majlis.Date.ToString("yyyy-MM-dd"))} at {Escape(majlis.Time)}</p>
            <p><a href="{Escape(url)}">View center details</a></p>
            """,
            $"""
            Assalamu alaikum {user.Name},

            {center.Name} has published a new majlis: {majlis.Title}
            {majlis.Description}
            Date: {majlis.Date:yyyy-MM-dd} at {majlis.Time}
            View center details: {url}
            """,
            cancellationToken);
    }

    public Task SendEventNotificationAsync(User user, Center center, EventAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl("center-details.html", new Dictionary<string, string?>
        {
            ["id"] = center.Id.ToString()
        });

        return SendAsync(
            user,
            $"New event update from {center.Name}",
            $"""
            <p>Assalamu alaikum {Escape(user.Name)},</p>
            <p>{Escape(center.Name)} has published a new event announcement: <strong>{Escape(announcement.Title)}</strong>.</p>
            <p>{Escape(announcement.Description)}</p>
            <p><a href="{Escape(url)}">Open the center page</a></p>
            """,
            $"""
            Assalamu alaikum {user.Name},

            {center.Name} has published a new event announcement: {announcement.Title}
            {announcement.Description}
            Open the center page: {url}
            """,
            cancellationToken);
    }

    private Task SendAsync(User user, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        var message = new EmailDispatchMessage
        {
            ToEmail = user.Email,
            ToName = user.Name,
            FromEmail = smtpSettings.FromEmail,
            FromName = smtpSettings.FromName,
            Subject = subject,
            HtmlBody = WrapHtml(htmlBody),
            TextBody = textBody.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        return emailDeliveryService.SendAsync(message, cancellationToken);
    }

    private string BuildFrontendUrl(string path, IReadOnlyDictionary<string, string?> query)
    {
        var origin = ResolveFrontendOrigin();
        var builder = new UriBuilder(new Uri(new Uri(origin), path));

        var queryBuilder = new StringBuilder();
        foreach (var pair in query.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
        {
            if (queryBuilder.Length > 0)
            {
                queryBuilder.Append('&');
            }

            queryBuilder.Append(Uri.EscapeDataString(pair.Key));
            queryBuilder.Append('=');
            queryBuilder.Append(Uri.EscapeDataString(pair.Value!));
        }

        builder.Query = queryBuilder.ToString();
        return builder.Uri.ToString();
    }

    private string ResolveFrontendOrigin()
    {
        if (Uri.TryCreate(frontendSettings.PublicOrigin?.Trim(), UriKind.Absolute, out var publicOrigin))
        {
            return publicOrigin.AbsoluteUri.TrimEnd('/') + "/";
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host.Value}/";
        }

        return "http://127.0.0.1:5500/";
    }

    private static string WrapHtml(string content)
    {
        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <body style="font-family:Arial,sans-serif;background:#f5efe6;color:#1f2a37;padding:24px;">
            <div style="max-width:640px;margin:0 auto;background:#ffffff;border-radius:16px;padding:32px;box-shadow:0 12px 32px rgba(13,37,53,0.12);">
                <h2 style="margin-top:0;color:#0d2535;">NoorLocator</h2>
                {content}
                <p style="margin-top:32px;color:#6b7280;font-size:14px;">Sent by NoorLocator</p>
            </div>
        </body>
        </html>
        """;
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);
}
