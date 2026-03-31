using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;

namespace NoorLocator.Infrastructure.Services.Email;

public class EmailDeliveryService(
    IOptions<SmtpSettings> smtpOptions,
    IHostEnvironment environment,
    EmailDispatchRecorder recorder) : IEmailDeliveryService
{
    private readonly SmtpSettings smtpSettings = smtpOptions.Value;

    public async Task SendAsync(EmailDispatchMessage message, CancellationToken cancellationToken = default)
    {
        recorder.Record(message);

        if (CanUseSmtp())
        {
            using var smtpClient = new SmtpClient(smtpSettings.Host, smtpSettings.Port)
            {
                EnableSsl = smtpSettings.UseSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password)
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(message.FromEmail, message.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(message.ToEmail, message.ToName));
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

            cancellationToken.ThrowIfCancellationRequested();
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            return;
        }

        if (!smtpSettings.WriteToPickupDirectoryWhenDisabled && !environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("SMTP settings are required for outbound email in this environment.");
        }

        var pickupDirectory = ResolvePickupDirectory();
        Directory.CreateDirectory(pickupDirectory);

        var payload = JsonSerializer.Serialize(
            new
            {
                message.FromEmail,
                message.FromName,
                message.ToEmail,
                message.ToName,
                message.Subject,
                message.TextBody,
                message.HtmlBody,
                message.CreatedAtUtc
            },
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json";
        await File.WriteAllTextAsync(Path.Combine(pickupDirectory, fileName), payload, cancellationToken);
    }

    private bool CanUseSmtp()
    {
        return !string.IsNullOrWhiteSpace(smtpSettings.Host)
               && !string.IsNullOrWhiteSpace(smtpSettings.Username)
               && !string.IsNullOrWhiteSpace(smtpSettings.Password);
    }

    private string ResolvePickupDirectory()
    {
        if (Path.IsPathRooted(smtpSettings.PickupDirectory))
        {
            return smtpSettings.PickupDirectory;
        }

        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, smtpSettings.PickupDirectory));
    }
}
