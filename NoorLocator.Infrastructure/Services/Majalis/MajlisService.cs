using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.Infrastructure.Services.Majalis;

public class MajlisService(
    NoorLocatorDbContext dbContext,
    IManagerCenterAccessService managerCenterAccessService,
    IMediaStorageService mediaStorageService,
    AuditLogger auditLogger) : IMajlisService
{
    private const string StorageCategory = "majalis";

    public async Task<OperationResult> CreateMajlisAsync(
        CreateMajlisDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken))
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, request.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only manage majalis for assigned centers.", 403);
        }

        var distinctLanguageIds = await ValidateLanguagesAsync(request.LanguageIds, cancellationToken);
        if (distinctLanguageIds is null)
        {
            return OperationResult.Failure("Majlis languages must come from the predefined language table.", 400);
        }

        string? imageUrl = null;
        if (image is not null)
        {
            var imageStoreResult = await mediaStorageService.SaveImageAsync(image, StorageCategory, cancellationToken);
            if (!imageStoreResult.Succeeded)
            {
                return OperationResult.Failure(imageStoreResult.Message, imageStoreResult.StatusCode);
            }

            imageUrl = imageStoreResult.Data!.PublicUrl;
        }

        var majlis = new Majlis
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = imageUrl,
            Date = request.Date.Date,
            Time = request.Time.Trim(),
            CenterId = request.CenterId,
            CreatedByManagerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Majalis.Add(majlis);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncMajlisLanguagesAsync(majlis.Id, distinctLanguageIds, cancellationToken);

        await auditLogger.WriteAsync(
            action: "MajlisCreated",
            entityName: nameof(Majlis),
            entityId: majlis.Id.ToString(),
            userId: userId,
            metadata: new
            {
                majlis.CenterId,
                majlis.Title,
                majlis.Date,
                majlis.Time,
                majlis.ImageUrl,
                LanguageIds = distinctLanguageIds
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Majlis created successfully.", 201);
    }

    public async Task<OperationResult> DeleteMajlisAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var majlis = await dbContext.Majalis
            .Include(currentMajlis => currentMajlis.MajlisLanguages)
            .SingleOrDefaultAsync(currentMajlis => currentMajlis.Id == id, cancellationToken);

        if (majlis is null)
        {
            return OperationResult.Failure("Majlis not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, majlis.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only manage majalis for assigned centers.", 403);
        }

        dbContext.MajlisLanguages.RemoveRange(majlis.MajlisLanguages);
        dbContext.Majalis.Remove(majlis);
        await dbContext.SaveChangesAsync(cancellationToken);
        await mediaStorageService.DeleteFileAsync(majlis.ImageUrl, cancellationToken);

        await auditLogger.WriteAsync(
            action: "MajlisDeleted",
            entityName: nameof(Majlis),
            entityId: id.ToString(),
            userId: userId,
            metadata: new
            {
                majlis.CenterId,
                majlis.Title
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Majlis deleted successfully.");
    }

    public async Task<OperationResult<MajlisDto>> GetMajlisByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var majlis = await dbContext.Majalis
            .AsNoTracking()
            .Include(currentMajlis => currentMajlis.Center)
            .Include(currentMajlis => currentMajlis.MajlisLanguages)
                .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .SingleOrDefaultAsync(currentMajlis => currentMajlis.Id == id, cancellationToken);

        if (majlis is null)
        {
            return OperationResult<MajlisDto>.Failure("Majlis not found.", 404);
        }

        return OperationResult<MajlisDto>.Success(MapMajlis(majlis));
    }

    public async Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetMajalisAsync(int? centerId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Majalis
            .AsNoTracking()
            .Include(majlis => majlis.Center)
            .Include(majlis => majlis.MajlisLanguages)
                .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .AsQueryable();

        if (centerId.HasValue)
        {
            query = query.Where(majlis => majlis.CenterId == centerId.Value);
        }

        var majalis = await query
            .OrderBy(majlis => majlis.Date)
            .ThenBy(majlis => majlis.Time)
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<MajlisDto>>.Success(
            majalis.Select(MapMajlis).ToArray());
    }

    public async Task<OperationResult> UpdateMajlisAsync(
        int id,
        UpdateMajlisDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var majlis = await dbContext.Majalis
            .Include(currentMajlis => currentMajlis.MajlisLanguages)
            .SingleOrDefaultAsync(currentMajlis => currentMajlis.Id == id, cancellationToken);

        if (majlis is null)
        {
            return OperationResult.Failure("Majlis not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, majlis.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only manage majalis for assigned centers.", 403);
        }

        if (!await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken))
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        if (!await managerCenterAccessService.CanManageCenterAsync(userId, request.CenterId, isAdmin, cancellationToken))
        {
            return OperationResult.Failure("Managers can only move majalis to assigned centers.", 403);
        }

        var distinctLanguageIds = await ValidateLanguagesAsync(request.LanguageIds, cancellationToken);
        if (distinctLanguageIds is null)
        {
            return OperationResult.Failure("Majlis languages must come from the predefined language table.", 400);
        }

        string? replacementImageUrl = null;
        if (image is not null)
        {
            var imageStoreResult = await mediaStorageService.SaveImageAsync(image, StorageCategory, cancellationToken);
            if (!imageStoreResult.Succeeded)
            {
                return OperationResult.Failure(imageStoreResult.Message, imageStoreResult.StatusCode);
            }

            replacementImageUrl = imageStoreResult.Data!.PublicUrl;
        }

        var previousImageUrl = majlis.ImageUrl;
        majlis.Title = request.Title.Trim();
        majlis.Description = request.Description.Trim();
        majlis.Date = request.Date.Date;
        majlis.Time = request.Time.Trim();
        majlis.CenterId = request.CenterId;

        if (replacementImageUrl is not null)
        {
            majlis.ImageUrl = replacementImageUrl;
        }
        else if (request.RemoveImage)
        {
            majlis.ImageUrl = null;
        }

        await SyncMajlisLanguagesAsync(majlis.Id, distinctLanguageIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (replacementImageUrl is not null || request.RemoveImage)
        {
            await mediaStorageService.DeleteFileAsync(previousImageUrl, cancellationToken);
        }

        await auditLogger.WriteAsync(
            action: "MajlisUpdated",
            entityName: nameof(Majlis),
            entityId: majlis.Id.ToString(),
            userId: userId,
            metadata: new
            {
                majlis.CenterId,
                majlis.Title,
                majlis.Date,
                majlis.Time,
                majlis.ImageUrl,
                LanguageIds = distinctLanguageIds
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Majlis updated successfully.");
    }

    private async Task<int[]?> ValidateLanguagesAsync(IReadOnlyCollection<int> languageIds, CancellationToken cancellationToken)
    {
        var distinctLanguageIds = languageIds
            .Where(languageId => languageId > 0)
            .Distinct()
            .ToArray();

        if (distinctLanguageIds.Length == 0)
        {
            return Array.Empty<int>();
        }

        var knownLanguageCount = await dbContext.Languages
            .CountAsync(language => distinctLanguageIds.Contains(language.Id), cancellationToken);

        return knownLanguageCount == distinctLanguageIds.Length
            ? distinctLanguageIds
            : null;
    }

    private async Task SyncMajlisLanguagesAsync(int majlisId, IReadOnlyCollection<int> languageIds, CancellationToken cancellationToken)
    {
        var existingLinks = await dbContext.MajlisLanguages
            .Where(majlisLanguage => majlisLanguage.MajlisId == majlisId)
            .ToArrayAsync(cancellationToken);

        var desiredLanguageIds = languageIds.ToHashSet();
        var linksToRemove = existingLinks
            .Where(existingLink => !desiredLanguageIds.Contains(existingLink.LanguageId))
            .ToArray();

        if (linksToRemove.Length > 0)
        {
            dbContext.MajlisLanguages.RemoveRange(linksToRemove);
        }

        var existingLanguageIds = existingLinks
            .Select(existingLink => existingLink.LanguageId)
            .ToHashSet();

        var linksToAdd = desiredLanguageIds
            .Where(languageId => !existingLanguageIds.Contains(languageId))
            .Select(languageId => new MajlisLanguage
            {
                MajlisId = majlisId,
                LanguageId = languageId
            })
            .ToArray();

        if (linksToAdd.Length > 0)
        {
            dbContext.MajlisLanguages.AddRange(linksToAdd);
        }

        if (linksToRemove.Length > 0 || linksToAdd.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static MajlisDto MapMajlis(Majlis majlis)
    {
        return new MajlisDto
        {
            Id = majlis.Id,
            Title = majlis.Title,
            Description = majlis.Description,
            ImageUrl = majlis.ImageUrl,
            Date = majlis.Date,
            Time = majlis.Time,
            CenterId = majlis.CenterId,
            CenterName = majlis.Center?.Name ?? string.Empty,
            CenterCity = majlis.Center?.City ?? string.Empty,
            CenterCountry = majlis.Center?.Country ?? string.Empty,
            Languages = majlis.MajlisLanguages
                .Where(majlisLanguage => majlisLanguage.Language is not null)
                .Select(majlisLanguage => new LanguageDto
                {
                    Id = majlisLanguage.Language!.Id,
                    Name = majlisLanguage.Language.Name,
                    Code = majlisLanguage.Language.Code
                })
                .OrderBy(language => language.Name)
                .ToArray()
        };
    }
}
