namespace NoorLocator.Infrastructure.Services.Email;

public interface IEmailDeliveryService
{
    Task SendAsync(EmailDispatchMessage message, CancellationToken cancellationToken = default);
}
