using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Notifications.Dtos;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task NotifyMajlisCreatedAsync(Majlis majlis, CancellationToken cancellationToken = default);

    Task NotifyEventPublishedAsync(EventAnnouncement announcement, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<NotificationDto>>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken = default);

    Task<OperationResult<UnreadNotificationCountDto>> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    Task<OperationResult> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task<OperationResult> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}
