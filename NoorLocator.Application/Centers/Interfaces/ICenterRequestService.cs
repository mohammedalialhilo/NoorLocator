using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Centers.Interfaces;

public interface ICenterRequestService
{
    Task<OperationResult> CreateAsync(CreateCenterRequestDto request, int userId, CancellationToken cancellationToken = default);
}
