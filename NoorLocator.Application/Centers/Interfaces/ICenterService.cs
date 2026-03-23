using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Centers.Interfaces;

public interface ICenterService
{
    Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetCentersAsync(CenterLocationQueryDto query, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetNearestCentersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> SearchCentersAsync(CenterSearchQueryDto query, CancellationToken cancellationToken = default);

    Task<OperationResult<CenterDetailsDto>> GetCenterByIdAsync(int id, CenterLocationQueryDto query, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetCenterMajalisAsync(int centerId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<LanguageDto>>> GetCenterLanguagesAsync(int centerId, CancellationToken cancellationToken = default);
}
