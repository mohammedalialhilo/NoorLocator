using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Interfaces;

namespace NoorLocator.Infrastructure.Services.Majalis;

public class PlaceholderMajlisService : IMajlisService
{
    public Task<OperationResult> CreateMajlisAsync(CreateMajlisDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Majlis publishing is scaffolded. Role checks and persistence will be implemented in a later phase."));
    }

    public Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetMajalisAsync(int? centerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<MajlisDto> majalis = Array.Empty<MajlisDto>();

        return Task.FromResult(
            OperationResult<IReadOnlyCollection<MajlisDto>>.Success(
                majalis,
                "No majalis are available in the Phase 1 scaffold yet."));
    }
}
