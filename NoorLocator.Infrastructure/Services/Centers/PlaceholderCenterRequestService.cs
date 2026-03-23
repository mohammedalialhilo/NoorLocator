using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Centers;

public class PlaceholderCenterRequestService : ICenterRequestService
{
    public Task<OperationResult> CreateAsync(CreateCenterRequestDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Center request submission is scaffolded. Moderated persistence will be implemented in the next phase."));
    }
}
