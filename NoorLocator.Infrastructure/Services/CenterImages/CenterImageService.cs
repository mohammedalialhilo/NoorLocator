using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.CenterImages.Dtos;
using NoorLocator.Application.CenterImages.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.Infrastructure.Services.CenterImages;

public class CenterImageService(
    NoorLocatorDbContext dbContext,
    IManagerCenterAccessService managerCenterAccessService,
    IMediaStorageService mediaStorageService,
    AuditLogger auditLogger) : ICenterImageService
{
    private const string StorageCategory = "center-images";

    public async Task<OperationResult<IReadOnlyCollection<CenterImageDto>>> GetCenterImagesAsync(int centerId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Centers.AnyAsync(center => center.Id == centerId, cancellationToken))
        {
            return OperationResult<IReadOnlyCollection<CenterImageDto>>.Failure("Center not found.", 404);
        }

        var images = await dbContext.CenterImages
            .AsNoTracking()
            .Where(image => image.CenterId == centerId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenByDescending(image => image.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterImageDto>>.Success(images.Select(MapCenterImage).ToArray());
    }

    public async Task<OperationResult<CenterImageDto>> UploadCenterImageAsync(
        UploadCenterImageDto request,
        UploadFilePayload file,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken))
        {
            return OperationResult<CenterImageDto>.Failure("Center not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, request.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult<CenterImageDto>.Failure("Managers can only manage images for assigned centers.", 403);
        }

        var imageStoreResult = await mediaStorageService.SaveImageAsync(file, StorageCategory, cancellationToken);
        if (!imageStoreResult.Succeeded)
        {
            return OperationResult<CenterImageDto>.Failure(imageStoreResult.Message, imageStoreResult.StatusCode);
        }

        var storedImage = imageStoreResult.Data!;

        var shouldBePrimary = request.IsPrimary ||
                              !await dbContext.CenterImages.AnyAsync(image => image.CenterId == request.CenterId, cancellationToken);

        try
        {
            if (shouldBePrimary)
            {
                var existingPrimaryImages = await dbContext.CenterImages
                    .Where(image => image.CenterId == request.CenterId && image.IsPrimary)
                    .ToArrayAsync(cancellationToken);

                foreach (var existingPrimaryImage in existingPrimaryImages)
                {
                    existingPrimaryImage.IsPrimary = false;
                }
            }

            var centerImage = new CenterImage
            {
                CenterId = request.CenterId,
                ImageUrl = storedImage.PublicUrl,
                UploadedByManagerId = userId,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = shouldBePrimary
            };

            dbContext.CenterImages.Add(centerImage);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditLogger.WriteAsync(
                action: "CenterImageUploaded",
                entityName: nameof(CenterImage),
                entityId: centerImage.Id.ToString(),
                userId: userId,
                metadata: new
                {
                    centerImage.CenterId,
                    centerImage.ImageUrl,
                    centerImage.IsPrimary
                },
                cancellationToken: cancellationToken);

            return OperationResult<CenterImageDto>.Success(
                MapCenterImage(centerImage),
                "Center image uploaded successfully.",
                201);
        }
        catch
        {
            await mediaStorageService.DeleteFileAsync(storedImage.PublicUrl, cancellationToken);
            throw;
        }
    }

    public async Task<OperationResult<CenterImageDto>> SetPrimaryImageAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var centerImage = await dbContext.CenterImages
            .SingleOrDefaultAsync(image => image.Id == id, cancellationToken);

        if (centerImage is null)
        {
            return OperationResult<CenterImageDto>.Failure("Center image not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, centerImage.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult<CenterImageDto>.Failure("Managers can only manage images for assigned centers.", 403);
        }

        var centerImages = await dbContext.CenterImages
            .Where(image => image.CenterId == centerImage.CenterId)
            .ToArrayAsync(cancellationToken);

        foreach (var image in centerImages)
        {
            image.IsPrimary = image.Id == centerImage.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "CenterImagePrimaryUpdated",
            entityName: nameof(CenterImage),
            entityId: centerImage.Id.ToString(),
            userId: userId,
            metadata: new
            {
                centerImage.CenterId,
                centerImage.ImageUrl
            },
            cancellationToken: cancellationToken);

        return OperationResult<CenterImageDto>.Success(
            MapCenterImage(centerImage),
            "Primary center image updated successfully.");
    }

    public async Task<OperationResult> DeleteCenterImageAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var centerImage = await dbContext.CenterImages
            .SingleOrDefaultAsync(image => image.Id == id, cancellationToken);

        if (centerImage is null)
        {
            return OperationResult.Failure("Center image not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, centerImage.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only manage images for assigned centers.", 403);
        }

        var centerId = centerImage.CenterId;
        var removedPrimary = centerImage.IsPrimary;
        var removedImageUrl = centerImage.ImageUrl;
        CenterImage? nextPrimary = null;

        if (removedPrimary)
        {
            nextPrimary = await dbContext.CenterImages
                .Where(image => image.CenterId == centerId)
                .Where(image => image.Id != centerImage.Id)
                .OrderByDescending(image => image.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        dbContext.CenterImages.Remove(centerImage);

        if (nextPrimary is not null)
        {
            nextPrimary.IsPrimary = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await mediaStorageService.DeleteFileAsync(removedImageUrl, cancellationToken);

        await auditLogger.WriteAsync(
            action: "CenterImageDeleted",
            entityName: nameof(CenterImage),
            entityId: id.ToString(),
            userId: userId,
            metadata: new
            {
                centerId,
                removedPrimary
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center image deleted successfully.");
    }

    private static CenterImageDto MapCenterImage(CenterImage image)
    {
        return new CenterImageDto
        {
            Id = image.Id,
            CenterId = image.CenterId,
            ImageUrl = image.ImageUrl,
            CreatedAt = image.CreatedAt,
            IsPrimary = image.IsPrimary
        };
    }
}
