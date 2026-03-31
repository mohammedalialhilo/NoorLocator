using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Services.Email;

public interface INoorLocatorEmailService
{
    Task SendVerificationEmailAsync(User user, string token, CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken = default);

    Task SendPasswordChangedConfirmationAsync(User user, CancellationToken cancellationToken = default);

    Task SendMajlisNotificationAsync(User user, Center center, Majlis majlis, CancellationToken cancellationToken = default);

    Task SendEventNotificationAsync(User user, Center center, EventAnnouncement announcement, CancellationToken cancellationToken = default);
}
