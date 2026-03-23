using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Centers;

public class CenterRequestService(NoorLocatorDbContext dbContext) : ICenterRequestService
{
    public async Task<OperationResult> CreateAsync(CreateCenterRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
        {
            return OperationResult.Failure("Authenticated user was not found.", 404);
        }

        var duplicateExists = await dbContext.CenterRequests.AnyAsync(
            centerRequest => centerRequest.RequestedByUserId == userId &&
                             centerRequest.Name == request.Name &&
                             centerRequest.City == request.City &&
                             centerRequest.Country == request.Country &&
                             centerRequest.Status == ModerationStatus.Pending,
            cancellationToken);

        if (duplicateExists)
        {
            return OperationResult.Failure("A pending center request for this center already exists.", 409);
        }

        dbContext.CenterRequests.Add(new CenterRequest
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description.Trim(),
            RequestedByUserId = userId,
            Status = ModerationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Accepted("Center request submitted for admin review.");
    }
}
