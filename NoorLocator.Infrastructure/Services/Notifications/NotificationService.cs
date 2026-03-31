using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Notifications.Dtos;
using NoorLocator.Application.Notifications.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Email;

namespace NoorLocator.Infrastructure.Services.Notifications;

public class NotificationService(
    NoorLocatorDbContext dbContext,
    INoorLocatorEmailService emailService) : INotificationService
{
    public Task NotifyMajlisCreatedAsync(Majlis majlis, CancellationToken cancellationToken = default)
    {
        return NotifyCenterAudienceAsync(
            centerId: majlis.CenterId,
            actorUserId: majlis.CreatedByManagerId,
            type: NotificationType.Majlis,
            deduplicationKey: $"majlis-created:{majlis.Id}",
            title: $"New majlis: {majlis.Title}",
            messageFactory: center => $"{center.Name} has published a new majlis. Open the center page to see the schedule and details.",
            linkUrlFactory: center => $"center-details.html?id={center.Id}",
            relatedEntityType: nameof(Majlis),
            relatedEntityId: majlis.Id,
            emailDispatcher: (user, center) => emailService.SendMajlisNotificationAsync(user, center, majlis, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public Task NotifyEventPublishedAsync(EventAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        return NotifyCenterAudienceAsync(
            centerId: announcement.CenterId,
            actorUserId: announcement.CreatedByManagerId,
            type: NotificationType.Event,
            deduplicationKey: $"event-published:{announcement.Id}",
            title: $"New event update: {announcement.Title}",
            messageFactory: center => $"{center.Name} has published a new event announcement. Open the center page to read the full update.",
            linkUrlFactory: center => $"center-details.html?id={center.Id}",
            relatedEntityType: nameof(EventAnnouncement),
            relatedEntityId: announcement.Id,
            emailDispatcher: (user, center) => emailService.SendEventNotificationAsync(user, center, announcement, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyCollection<NotificationDto>>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<NotificationDto>>.Success(
            notifications.Select(MapNotification).ToArray());
    }

    public async Task<OperationResult<UnreadNotificationCountDto>> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Notifications.CountAsync(
            notification => notification.UserId == userId && !notification.IsRead,
            cancellationToken);

        return OperationResult<UnreadNotificationCountDto>.Success(new UnreadNotificationCountDto
        {
            Count = count
        });
    }

    public async Task<OperationResult> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(
                currentNotification => currentNotification.Id == notificationId && currentNotification.UserId == userId,
                cancellationToken);

        if (notification is null)
        {
            return OperationResult.Failure("Notification not found.", 404);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Success("Notification marked as read.");
    }

    public async Task<OperationResult> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToArrayAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
        }

        if (notifications.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Success("All notifications marked as read.");
    }

    private async Task NotifyCenterAudienceAsync(
        int centerId,
        int actorUserId,
        NotificationType type,
        string deduplicationKey,
        string title,
        Func<Center, string> messageFactory,
        Func<Center, string> linkUrlFactory,
        string relatedEntityType,
        int relatedEntityId,
        Func<User, Center, Task> emailDispatcher,
        CancellationToken cancellationToken)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == centerId, cancellationToken);

        if (center is null)
        {
            return;
        }

        var visitedUserIds = await dbContext.UserCenterVisits
            .AsNoTracking()
            .Where(visit => visit.CenterId == centerId)
            .Select(visit => visit.UserId)
            .ToArrayAsync(cancellationToken);

        var subscribedUserIds = await dbContext.UserCenterSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.CenterId == centerId)
            .Select(subscription => subscription.UserId)
            .ToArrayAsync(cancellationToken);

        var recipientIds = visitedUserIds
            .Concat(subscribedUserIds)
            .Where(userId => userId != actorUserId)
            .Distinct()
            .ToArray();

        if (recipientIds.Length == 0)
        {
            return;
        }

        var existingNotificationUserIds = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.DeduplicationKey == deduplicationKey && recipientIds.Contains(notification.UserId))
            .Select(notification => notification.UserId)
            .ToArrayAsync(cancellationToken);

        var subscriptionMap = await dbContext.UserCenterSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.CenterId == centerId && recipientIds.Contains(subscription.UserId))
            .ToDictionaryAsync(subscription => subscription.UserId, cancellationToken);

        var users = await dbContext.Users
            .Include(user => user.NotificationPreference)
            .Where(user => recipientIds.Contains(user.Id) && user.IsEmailVerified)
            .ToArrayAsync(cancellationToken);

        foreach (var user in users.Where(user => !existingNotificationUserIds.Contains(user.Id)))
        {
            var preferences = user.NotificationPreference ?? new UserNotificationPreference();
            var notificationsEnabledForType = type switch
            {
                NotificationType.Majlis => preferences.MajlisNotificationsEnabled,
                NotificationType.Event => preferences.EventNotificationsEnabled,
                _ => true
            };

            if (!preferences.CenterUpdatesEnabled || !notificationsEnabledForType)
            {
                continue;
            }

            var subscription = subscriptionMap.GetValueOrDefault(user.Id);
            var allowAppNotification = preferences.AppNotificationsEnabled && (subscription?.IsAppNotificationsEnabled ?? true);
            var allowEmailNotification = preferences.EmailNotificationsEnabled && (subscription?.IsEmailNotificationsEnabled ?? true);

            Notification? notification = null;
            if (allowAppNotification)
            {
                notification = new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = messageFactory(center),
                    Type = type,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    LinkUrl = linkUrlFactory(center),
                    IsRead = false,
                    DeduplicationKey = deduplicationKey,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Notifications.Add(notification);
            }

            if (allowEmailNotification)
            {
                await emailDispatcher(user, center);
                if (notification is not null)
                {
                    notification.SentByEmail = true;
                    notification.EmailSentAtUtc = DateTime.UtcNow;
                }
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static NotificationDto MapNotification(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            LinkUrl = notification.LinkUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            SentByEmail = notification.SentByEmail
        };
    }
}
