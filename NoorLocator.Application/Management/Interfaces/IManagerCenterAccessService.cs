using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Dtos;

namespace NoorLocator.Application.Management.Interfaces;

public interface IManagerCenterAccessService
{
    Task<bool> CanManageCenterAsync(int userId, int centerId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<ManagedCenterDto>>> GetManagedCentersAsync(int userId, bool isAdmin, CancellationToken cancellationToken = default);
}
