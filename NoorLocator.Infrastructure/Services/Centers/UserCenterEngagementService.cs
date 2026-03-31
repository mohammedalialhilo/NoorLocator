using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Centers;

public class UserCenterEngagementService(NoorLocatorDbContext dbContext) : IUserCenterEngagementService
{
    public async Task<OperationResult<IReadOnlyCollection<CenterSubscriptionDto>>> GetSubscriptionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.UserCenterSubscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Center)
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterSubscriptionDto>>.Success(
            subscriptions.Select(subscription => MapSubscription(subscription)).ToArray());
    }

    public async Task<OperationResult<CenterSubscriptionDto>> SubscribeAsync(int userId, int centerId, CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == centerId, cancellationToken);

        if (center is null)
        {
            return OperationResult<CenterSubscriptionDto>.Failure("Center not found.", 404);
        }

        var subscription = await dbContext.UserCenterSubscriptions
            .SingleOrDefaultAsync(
                currentSubscription => currentSubscription.UserId == userId && currentSubscription.CenterId == centerId,
                cancellationToken);

        if (subscription is null)
        {
            subscription = new UserCenterSubscription
            {
                UserId = userId,
                CenterId = centerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsAppNotificationsEnabled = true,
                IsEmailNotificationsEnabled = true
            };

            dbContext.UserCenterSubscriptions.Add(subscription);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<CenterSubscriptionDto>.Success(
            MapSubscription(subscription, center.Name),
            "You will now receive updates from this center.");
    }

    public async Task<OperationResult> TrackVisitAsync(int userId, int centerId, string source, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Centers.AnyAsync(center => center.Id == centerId, cancellationToken))
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        var visit = await dbContext.UserCenterVisits
            .SingleOrDefaultAsync(currentVisit => currentVisit.UserId == userId && currentVisit.CenterId == centerId, cancellationToken);

        if (visit is null)
        {
            dbContext.UserCenterVisits.Add(new UserCenterVisit
            {
                UserId = userId,
                CenterId = centerId,
                Source = string.IsNullOrWhiteSpace(source) ? "page_view" : source.Trim(),
                VisitedAtUtc = DateTime.UtcNow,
                VisitCount = 1,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            visit.Source = string.IsNullOrWhiteSpace(source) ? visit.Source : source.Trim();
            visit.VisitedAtUtc = DateTime.UtcNow;
            visit.VisitCount += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Center visit recorded.");
    }

    public async Task<OperationResult> UnsubscribeAsync(int userId, int centerId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.UserCenterSubscriptions
            .SingleOrDefaultAsync(
                currentSubscription => currentSubscription.UserId == userId && currentSubscription.CenterId == centerId,
                cancellationToken);

        if (subscription is null)
        {
            return OperationResult.Success("Center notifications are already disabled.");
        }

        dbContext.UserCenterSubscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Center notifications have been disabled.");
    }

    private static CenterSubscriptionDto MapSubscription(UserCenterSubscription subscription, string? centerName = null)
    {
        return new CenterSubscriptionDto
        {
            CenterId = subscription.CenterId,
            CenterName = centerName ?? subscription.Center?.Name ?? string.Empty,
            IsAppNotificationsEnabled = subscription.IsAppNotificationsEnabled,
            IsEmailNotificationsEnabled = subscription.IsEmailNotificationsEnabled,
            CreatedAt = subscription.CreatedAt
        };
    }
}
