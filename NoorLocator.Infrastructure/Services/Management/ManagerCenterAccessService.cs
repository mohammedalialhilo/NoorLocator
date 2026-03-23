using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Management;

public class ManagerCenterAccessService(NoorLocatorDbContext dbContext) : IManagerCenterAccessService
{
    public async Task<bool> CanManageCenterAsync(int userId, int centerId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return await dbContext.Centers.AnyAsync(center => center.Id == centerId, cancellationToken);
        }

        return await dbContext.CenterManagers.AnyAsync(
            centerManager =>
                centerManager.UserId == userId &&
                centerManager.CenterId == centerId &&
                centerManager.Approved,
            cancellationToken);
    }

    public async Task<OperationResult<IReadOnlyCollection<ManagedCenterDto>>> GetManagedCentersAsync(
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Centers
            .AsNoTracking()
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(center =>
                center.CenterManagers.Any(centerManager =>
                    centerManager.UserId == userId &&
                    centerManager.Approved));
        }

        var centers = await query
            .OrderBy(center => center.Country)
            .ThenBy(center => center.City)
            .ThenBy(center => center.Name)
            .Select(center => new ManagedCenterDto
            {
                Id = center.Id,
                Name = center.Name,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Description = center.Description
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<ManagedCenterDto>>.Success(centers);
    }
}
