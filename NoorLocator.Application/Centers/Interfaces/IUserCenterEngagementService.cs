using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Centers.Interfaces;

public interface IUserCenterEngagementService
{
    Task<OperationResult> TrackVisitAsync(int userId, int centerId, string source, CancellationToken cancellationToken = default);

    Task<OperationResult<CenterSubscriptionDto>> SubscribeAsync(int userId, int centerId, CancellationToken cancellationToken = default);

    Task<OperationResult> UnsubscribeAsync(int userId, int centerId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<CenterSubscriptionDto>>> GetSubscriptionsAsync(int userId, CancellationToken cancellationToken = default);
}
