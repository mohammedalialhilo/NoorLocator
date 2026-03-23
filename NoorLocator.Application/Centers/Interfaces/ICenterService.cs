using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Centers.Interfaces;

public interface ICenterService
{
    Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetCentersAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetNearestCentersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

    Task<OperationResult<CenterDetailsDto>> GetCenterByIdAsync(int id, CancellationToken cancellationToken = default);
}
