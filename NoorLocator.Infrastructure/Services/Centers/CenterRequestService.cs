using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.Infrastructure.Services.Centers;

public class CenterRequestService(NoorLocatorDbContext dbContext, AuditLogger auditLogger) : ICenterRequestService
{
    public async Task<OperationResult> CreateAsync(CreateCenterRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
        {
            return OperationResult.Failure("Authenticated user was not found.", 404);
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var normalizedCity = request.City.Trim().ToLowerInvariant();
        var normalizedCountry = request.Country.Trim().ToLowerInvariant();
        var minLatitude = request.Latitude - 0.02m;
        var maxLatitude = request.Latitude + 0.02m;
        var minLongitude = request.Longitude - 0.02m;
        var maxLongitude = request.Longitude + 0.02m;

        var duplicatePublishedCenterExists = await dbContext.Centers.AnyAsync(
            center =>
                center.Name.ToLower() == normalizedName &&
                ((center.City.ToLower() == normalizedCity && center.Country.ToLower() == normalizedCountry) ||
                 (center.Latitude >= minLatitude && center.Latitude <= maxLatitude &&
                  center.Longitude >= minLongitude && center.Longitude <= maxLongitude)),
            cancellationToken);

        if (duplicatePublishedCenterExists)
        {
            return OperationResult.Failure("A similar center already exists in NoorLocator.", 409);
        }

        var duplicatePendingRequestExists = await dbContext.CenterRequests.AnyAsync(
            centerRequest =>
                centerRequest.Status == ModerationStatus.Pending &&
                centerRequest.Name.ToLower() == normalizedName &&
                ((centerRequest.City.ToLower() == normalizedCity && centerRequest.Country.ToLower() == normalizedCountry) ||
                 (centerRequest.Latitude >= minLatitude && centerRequest.Latitude <= maxLatitude &&
                  centerRequest.Longitude >= minLongitude && centerRequest.Longitude <= maxLongitude)),
            cancellationToken);

        if (duplicatePendingRequestExists)
        {
            return OperationResult.Failure("A similar pending center request already exists.", 409);
        }

        var centerRequest = new CenterRequest
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
        };

        dbContext.CenterRequests.Add(centerRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "CenterRequestSubmitted",
            entityName: nameof(CenterRequest),
            entityId: centerRequest.Id.ToString(),
            userId: userId,
            metadata: new
            {
                centerRequest.Name,
                centerRequest.City,
                centerRequest.Country,
                centerRequest.Latitude,
                centerRequest.Longitude,
                centerRequest.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Accepted("Center request submitted for admin review.");
    }

    public async Task<OperationResult<IReadOnlyCollection<CenterRequestSummaryDto>>> GetMineAsync(int userId, CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.CenterRequests
            .AsNoTracking()
            .Where(centerRequest => centerRequest.RequestedByUserId == userId)
            .OrderByDescending(centerRequest => centerRequest.CreatedAt)
            .Select(centerRequest => new CenterRequestSummaryDto
            {
                Id = centerRequest.Id,
                Name = centerRequest.Name,
                Address = centerRequest.Address,
                City = centerRequest.City,
                Country = centerRequest.Country,
                Latitude = centerRequest.Latitude,
                Longitude = centerRequest.Longitude,
                Description = centerRequest.Description,
                Status = centerRequest.Status,
                CreatedAt = centerRequest.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterRequestSummaryDto>>.Success(requests);
    }
}
