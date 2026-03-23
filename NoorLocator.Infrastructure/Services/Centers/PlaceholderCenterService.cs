using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Centers;

public class PlaceholderCenterService : ICenterService
{
    public Task<OperationResult<CenterDetailsDto>> GetCenterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult<CenterDetailsDto>.Failure(
                "Center details will become available once centers are approved and published.",
                404));
    }

    public Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetCentersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CenterSummaryDto> centers = Array.Empty<CenterSummaryDto>();

        return Task.FromResult(
            OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(
                centers,
                "Phase 1 scaffold is ready. Approved centers will appear here in Phase 2."));
    }

    public Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetNearestCentersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CenterSummaryDto> centers = Array.Empty<CenterSummaryDto>();

        return Task.FromResult(
            OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(
                centers,
                "Nearest-center calculation is reserved for Phase 2 implementation."));
    }
}
