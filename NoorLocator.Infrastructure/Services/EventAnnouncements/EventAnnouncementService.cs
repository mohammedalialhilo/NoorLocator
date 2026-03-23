using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.EventAnnouncements.Dtos;
using NoorLocator.Application.EventAnnouncements.Interfaces;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.Infrastructure.Services.EventAnnouncements;

public class EventAnnouncementService(
    NoorLocatorDbContext dbContext,
    IManagerCenterAccessService managerCenterAccessService,
    IMediaStorageService mediaStorageService,
    AuditLogger auditLogger) : IEventAnnouncementService
{
    private const string StorageCategory = "event-announcements";

    public async Task<OperationResult<IReadOnlyCollection<EventAnnouncementDto>>> GetAnnouncementsAsync(
        int centerId,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == centerId, cancellationToken);

        if (center is null)
        {
            return OperationResult<IReadOnlyCollection<EventAnnouncementDto>>.Failure("Center not found.", 404);
        }

        var canViewUnpublished = userId.HasValue &&
                                 await managerCenterAccessService.CanManageCenterAsync(userId.Value, centerId, isAdmin, cancellationToken);

        var query = dbContext.EventAnnouncements
            .AsNoTracking()
            .Where(announcement => announcement.CenterId == centerId);

        if (!canViewUnpublished)
        {
            query = query.Where(announcement => announcement.Status == EventAnnouncementStatus.Published);
        }

        var announcements = await query
            .OrderByDescending(announcement => announcement.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<EventAnnouncementDto>>.Success(
            announcements
                .Select(announcement => MapAnnouncement(announcement, center.Name))
                .ToArray());
    }

    public async Task<OperationResult<EventAnnouncementDto>> GetAnnouncementByIdAsync(
        int id,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var announcement = await dbContext.EventAnnouncements
            .AsNoTracking()
            .Include(currentAnnouncement => currentAnnouncement.Center)
            .SingleOrDefaultAsync(currentAnnouncement => currentAnnouncement.Id == id, cancellationToken);

        if (announcement is null)
        {
            return OperationResult<EventAnnouncementDto>.Failure("Announcement not found.", 404);
        }

        if (announcement.Status != EventAnnouncementStatus.Published)
        {
            var canViewUnpublished = userId.HasValue &&
                                     await managerCenterAccessService.CanManageCenterAsync(
                                         userId.Value,
                                         announcement.CenterId,
                                         isAdmin,
                                         cancellationToken);

            if (!canViewUnpublished)
            {
                return OperationResult<EventAnnouncementDto>.Failure("Announcement not found.", 404);
            }
        }

        return OperationResult<EventAnnouncementDto>.Success(MapAnnouncement(announcement));
    }

    public async Task<OperationResult<EventAnnouncementDto>> CreateAnnouncementAsync(
        CreateEventAnnouncementDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == request.CenterId, cancellationToken);

        if (center is null)
        {
            return OperationResult<EventAnnouncementDto>.Failure("Center not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, request.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult<EventAnnouncementDto>.Failure("Managers can only publish announcements for assigned centers.", 403);
        }

        string? imageUrl = null;
        if (image is not null)
        {
            var imageStoreResult = await mediaStorageService.SaveImageAsync(image, StorageCategory, cancellationToken);
            if (!imageStoreResult.Succeeded)
            {
                return OperationResult<EventAnnouncementDto>.Failure(imageStoreResult.Message, imageStoreResult.StatusCode);
            }

            imageUrl = imageStoreResult.Data!.PublicUrl;
        }

        var announcement = new EventAnnouncement
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = imageUrl,
            CenterId = request.CenterId,
            CreatedByManagerId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = request.Status
        };

        dbContext.EventAnnouncements.Add(announcement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "EventAnnouncementCreated",
            entityName: nameof(EventAnnouncement),
            entityId: announcement.Id.ToString(),
            userId: userId,
            metadata: new
            {
                announcement.CenterId,
                announcement.Title,
                announcement.Status,
                announcement.ImageUrl
            },
            cancellationToken: cancellationToken);

        return OperationResult<EventAnnouncementDto>.Success(
            MapAnnouncement(announcement, center.Name),
            "Announcement created successfully.",
            201);
    }

    public async Task<OperationResult<EventAnnouncementDto>> UpdateAnnouncementAsync(
        int id,
        UpdateEventAnnouncementDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var announcement = await dbContext.EventAnnouncements
            .Include(currentAnnouncement => currentAnnouncement.Center)
            .SingleOrDefaultAsync(currentAnnouncement => currentAnnouncement.Id == id, cancellationToken);

        if (announcement is null)
        {
            return OperationResult<EventAnnouncementDto>.Failure("Announcement not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, announcement.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult<EventAnnouncementDto>.Failure("Managers can only edit announcements for assigned centers.", 403);
        }

        var targetCenter = await dbContext.Centers
            .AsNoTracking()
            .SingleOrDefaultAsync(center => center.Id == request.CenterId, cancellationToken);

        if (targetCenter is null)
        {
            return OperationResult<EventAnnouncementDto>.Failure("Center not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, request.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult<EventAnnouncementDto>.Failure("Managers can only move announcements to assigned centers.", 403);
        }

        string? replacementImageUrl = null;
        if (image is not null)
        {
            var imageStoreResult = await mediaStorageService.SaveImageAsync(image, StorageCategory, cancellationToken);
            if (!imageStoreResult.Succeeded)
            {
                return OperationResult<EventAnnouncementDto>.Failure(imageStoreResult.Message, imageStoreResult.StatusCode);
            }

            replacementImageUrl = imageStoreResult.Data!.PublicUrl;
        }

        var previousImageUrl = announcement.ImageUrl;
        announcement.Title = request.Title.Trim();
        announcement.Description = request.Description.Trim();
        announcement.CenterId = request.CenterId;
        announcement.Status = request.Status;

        if (replacementImageUrl is not null)
        {
            announcement.ImageUrl = replacementImageUrl;
        }
        else if (request.RemoveImage)
        {
            announcement.ImageUrl = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (replacementImageUrl is not null || request.RemoveImage)
        {
            await mediaStorageService.DeleteFileAsync(previousImageUrl, cancellationToken);
        }

        await auditLogger.WriteAsync(
            action: "EventAnnouncementUpdated",
            entityName: nameof(EventAnnouncement),
            entityId: announcement.Id.ToString(),
            userId: userId,
            metadata: new
            {
                announcement.CenterId,
                announcement.Title,
                announcement.Status,
                announcement.ImageUrl
            },
            cancellationToken: cancellationToken);

        return OperationResult<EventAnnouncementDto>.Success(
            MapAnnouncement(announcement, targetCenter.Name),
            "Announcement updated successfully.");
    }

    public async Task<OperationResult> DeleteAnnouncementAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var announcement = await dbContext.EventAnnouncements
            .SingleOrDefaultAsync(currentAnnouncement => currentAnnouncement.Id == id, cancellationToken);

        if (announcement is null)
        {
            return OperationResult.Failure("Announcement not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, announcement.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only delete announcements for assigned centers.", 403);
        }

        dbContext.EventAnnouncements.Remove(announcement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await mediaStorageService.DeleteFileAsync(announcement.ImageUrl, cancellationToken);

        await auditLogger.WriteAsync(
            action: "EventAnnouncementDeleted",
            entityName: nameof(EventAnnouncement),
            entityId: announcement.Id.ToString(),
            userId: userId,
            metadata: new
            {
                announcement.CenterId,
                announcement.Title,
                announcement.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Announcement deleted successfully.");
    }

    private static EventAnnouncementDto MapAnnouncement(EventAnnouncement announcement, string? centerName = null)
    {
        return new EventAnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Description = announcement.Description,
            ImageUrl = announcement.ImageUrl,
            CenterId = announcement.CenterId,
            CenterName = centerName ?? announcement.Center?.Name ?? string.Empty,
            CreatedByManagerId = announcement.CreatedByManagerId,
            CreatedAt = announcement.CreatedAt,
            Status = announcement.Status
        };
    }
}
