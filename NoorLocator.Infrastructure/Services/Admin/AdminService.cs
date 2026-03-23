using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.Infrastructure.Services.Admin;

public class AdminService(
    NoorLocatorDbContext dbContext,
    AuditLogger auditLogger) : IAdminService
{
    public async Task<OperationResult> ApproveCenterLanguageSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.CenterLanguageSuggestions
            .Include(currentSuggestion => currentSuggestion.Center)
            .Include(currentSuggestion => currentSuggestion.Language)
            .SingleOrDefaultAsync(currentSuggestion => currentSuggestion.Id == id, cancellationToken);

        if (suggestion is null)
        {
            return OperationResult.Failure("Center language suggestion not found.", 404);
        }

        if (suggestion.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending center language suggestions can be approved.", 409);
        }

        var exists = await dbContext.CenterLanguages.AnyAsync(
            centerLanguage => centerLanguage.CenterId == suggestion.CenterId && centerLanguage.LanguageId == suggestion.LanguageId,
            cancellationToken);

        if (!exists)
        {
            dbContext.CenterLanguages.Add(new CenterLanguage
            {
                CenterId = suggestion.CenterId,
                LanguageId = suggestion.LanguageId
            });
        }

        suggestion.Status = ModerationStatus.Approved;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminApprovedCenterLanguageSuggestion",
            entityName: nameof(CenterLanguageSuggestion),
            entityId: suggestion.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                suggestion.CenterId,
                suggestion.LanguageId,
                suggestion.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center language suggestion approved.");
    }

    public async Task<OperationResult> ApproveCenterRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var centerRequest = await dbContext.CenterRequests
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (centerRequest is null)
        {
            return OperationResult.Failure("Center request not found.", 404);
        }

        if (centerRequest.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending center requests can be approved.", 409);
        }

        if (await SimilarCenterExistsAsync(
                0,
                centerRequest.Name,
                centerRequest.City,
                centerRequest.Country,
                centerRequest.Latitude,
                centerRequest.Longitude,
                cancellationToken))
        {
            return OperationResult.Failure("A similar center already exists in NoorLocator.", 409);
        }

        var center = new Center
        {
            Name = centerRequest.Name,
            Address = centerRequest.Address,
            City = centerRequest.City,
            Country = centerRequest.Country,
            Latitude = centerRequest.Latitude,
            Longitude = centerRequest.Longitude,
            Description = centerRequest.Description,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Centers.Add(center);
        centerRequest.Status = ModerationStatus.Approved;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminApprovedCenterRequest",
            entityName: nameof(CenterRequest),
            entityId: centerRequest.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                centerRequest.Name,
                ApprovedCenterId = center.Id,
                centerRequest.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center request approved and published.");
    }

    public async Task<OperationResult> ApproveManagerRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var managerRequest = await dbContext.ManagerRequests
            .Include(currentRequest => currentRequest.User)
            .SingleOrDefaultAsync(currentRequest => currentRequest.Id == id, cancellationToken);

        if (managerRequest is null)
        {
            return OperationResult.Failure("Manager request not found.", 404);
        }

        if (managerRequest.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending manager requests can be approved.", 409);
        }

        var assignment = await dbContext.CenterManagers.SingleOrDefaultAsync(
            centerManager => centerManager.UserId == managerRequest.UserId &&
                             centerManager.CenterId == managerRequest.CenterId,
            cancellationToken);

        if (assignment is null)
        {
            dbContext.CenterManagers.Add(new CenterManager
            {
                UserId = managerRequest.UserId,
                CenterId = managerRequest.CenterId,
                Approved = true
            });
        }
        else
        {
            assignment.Approved = true;
        }

        managerRequest.Status = ModerationStatus.Approved;

        if (managerRequest.User is not null && managerRequest.User.Role == UserRole.User)
        {
            managerRequest.User.Role = UserRole.Manager;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminApprovedManagerRequest",
            entityName: nameof(ManagerRequest),
            entityId: managerRequest.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                managerRequest.UserId,
                managerRequest.CenterId,
                managerRequest.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Manager request approved.");
    }

    public async Task<OperationResult> DeleteCenterAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .Include(currentCenter => currentCenter.CenterManagers)
            .Include(currentCenter => currentCenter.ManagerRequests)
            .Include(currentCenter => currentCenter.CenterLanguages)
            .Include(currentCenter => currentCenter.CenterLanguageSuggestions)
            .Include(currentCenter => currentCenter.Majalis)
                .ThenInclude(majlis => majlis.MajlisLanguages)
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == id, cancellationToken);

        if (center is null)
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        var affectedManagerUserIds = center.CenterManagers
            .Where(centerManager => centerManager.Approved)
            .Select(centerManager => centerManager.UserId)
            .Distinct()
            .ToArray();

        var majlisLanguages = center.Majalis
            .SelectMany(majlis => majlis.MajlisLanguages)
            .ToArray();

        if (majlisLanguages.Length > 0)
        {
            dbContext.MajlisLanguages.RemoveRange(majlisLanguages);
        }

        if (center.Majalis.Count > 0)
        {
            dbContext.Majalis.RemoveRange(center.Majalis);
        }

        if (center.CenterManagers.Count > 0)
        {
            dbContext.CenterManagers.RemoveRange(center.CenterManagers);
        }

        if (center.ManagerRequests.Count > 0)
        {
            dbContext.ManagerRequests.RemoveRange(center.ManagerRequests);
        }

        if (center.CenterLanguages.Count > 0)
        {
            dbContext.CenterLanguages.RemoveRange(center.CenterLanguages);
        }

        if (center.CenterLanguageSuggestions.Count > 0)
        {
            dbContext.CenterLanguageSuggestions.RemoveRange(center.CenterLanguageSuggestions);
        }

        dbContext.Centers.Remove(center);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (affectedManagerUserIds.Length > 0)
        {
            var usersToEvaluate = await dbContext.Users
                .Where(user => affectedManagerUserIds.Contains(user.Id) && user.Role == UserRole.Manager)
                .ToArrayAsync(cancellationToken);

            foreach (var user in usersToEvaluate)
            {
                var hasApprovedAssignment = await dbContext.CenterManagers.AnyAsync(
                    centerManager => centerManager.UserId == user.Id && centerManager.Approved,
                    cancellationToken);

                if (!hasApprovedAssignment)
                {
                    user.Role = UserRole.User;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditLogger.WriteAsync(
            action: "AdminDeletedCenter",
            entityName: nameof(Center),
            entityId: id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                center.Name,
                center.City,
                center.Country
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center deleted successfully.");
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminAuditLogDto>>> GetAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        var auditLogs = await dbContext.AuditLogs
            .AsNoTracking()
            .Include(auditLog => auditLog.User)
            .OrderByDescending(auditLog => auditLog.CreatedAt)
            .Select(auditLog => new AdminAuditLogDto
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                UserName = auditLog.User != null ? auditLog.User.Name : string.Empty,
                UserEmail = auditLog.User != null ? auditLog.User.Email : string.Empty,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId ?? string.Empty,
                Metadata = auditLog.Metadata ?? string.Empty,
                IpAddress = auditLog.IpAddress ?? string.Empty,
                CreatedAt = auditLog.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminAuditLogDto>>.Success(auditLogs);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminCenterDto>>> GetCentersAsync(CancellationToken cancellationToken = default)
    {
        var centers = await dbContext.Centers
            .AsNoTracking()
            .OrderBy(center => center.Country)
            .ThenBy(center => center.City)
            .ThenBy(center => center.Name)
            .Select(center => new AdminCenterDto
            {
                Id = center.Id,
                Name = center.Name,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                Description = center.Description,
                ManagerCount = center.CenterManagers.Count(centerManager => centerManager.Approved),
                LanguageCount = center.CenterLanguages.Count(),
                MajlisCount = center.Majalis.Count()
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminCenterDto>>.Success(centers);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminCenterLanguageSuggestionDto>>> GetCenterLanguageSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var suggestions = await dbContext.CenterLanguageSuggestions
            .AsNoTracking()
            .Include(suggestion => suggestion.Center)
            .Include(suggestion => suggestion.Language)
            .Include(suggestion => suggestion.SuggestedByUser)
            .OrderBy(suggestion => suggestion.Status == ModerationStatus.Pending ? 0 : 1)
            .ThenByDescending(suggestion => suggestion.Id)
            .Select(suggestion => new AdminCenterLanguageSuggestionDto
            {
                Id = suggestion.Id,
                CenterId = suggestion.CenterId,
                CenterName = suggestion.Center != null ? suggestion.Center.Name : string.Empty,
                LanguageId = suggestion.LanguageId,
                LanguageName = suggestion.Language != null ? suggestion.Language.Name : string.Empty,
                LanguageCode = suggestion.Language != null ? suggestion.Language.Code : string.Empty,
                SuggestedByUserId = suggestion.SuggestedByUserId,
                SuggestedByUserName = suggestion.SuggestedByUser != null ? suggestion.SuggestedByUser.Name : string.Empty,
                SuggestedByUserEmail = suggestion.SuggestedByUser != null ? suggestion.SuggestedByUser.Email : string.Empty,
                Status = suggestion.Status
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminCenterLanguageSuggestionDto>>.Success(suggestions);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminCenterRequestDto>>> GetCenterRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.CenterRequests
            .AsNoTracking()
            .Include(centerRequest => centerRequest.RequestedByUser)
            .OrderBy(centerRequest => centerRequest.Status == ModerationStatus.Pending ? 0 : 1)
            .ThenByDescending(centerRequest => centerRequest.CreatedAt)
            .Select(centerRequest => new AdminCenterRequestDto
            {
                Id = centerRequest.Id,
                Name = centerRequest.Name,
                Address = centerRequest.Address,
                City = centerRequest.City,
                Country = centerRequest.Country,
                Latitude = centerRequest.Latitude,
                Longitude = centerRequest.Longitude,
                Description = centerRequest.Description,
                RequestedByUserId = centerRequest.RequestedByUserId,
                RequestedByUserName = centerRequest.RequestedByUser != null ? centerRequest.RequestedByUser.Name : string.Empty,
                RequestedByUserEmail = centerRequest.RequestedByUser != null ? centerRequest.RequestedByUser.Email : string.Empty,
                Status = centerRequest.Status,
                CreatedAt = centerRequest.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminCenterRequestDto>>.Success(requests);
    }

    public async Task<OperationResult<AdminDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = new AdminDashboardDto
        {
            PendingCenterRequests = await dbContext.CenterRequests.CountAsync(request => request.Status == ModerationStatus.Pending, cancellationToken),
            PendingManagerRequests = await dbContext.ManagerRequests.CountAsync(request => request.Status == ModerationStatus.Pending, cancellationToken),
            PendingCenterLanguageSuggestions = await dbContext.CenterLanguageSuggestions.CountAsync(suggestion => suggestion.Status == ModerationStatus.Pending, cancellationToken),
            PendingSuggestions = await dbContext.Suggestions.CountAsync(suggestion => suggestion.Status == SuggestionReviewStatus.Pending, cancellationToken),
            TotalUsers = await dbContext.Users.CountAsync(cancellationToken),
            TotalCenters = await dbContext.Centers.CountAsync(cancellationToken),
            TotalMajalis = await dbContext.Majalis.CountAsync(cancellationToken),
            TotalAuditLogs = await dbContext.AuditLogs.CountAsync(cancellationToken)
        };

        return OperationResult<AdminDashboardDto>.Success(dashboard);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminManagerRequestDto>>> GetManagerRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.ManagerRequests
            .AsNoTracking()
            .Include(managerRequest => managerRequest.User)
            .Include(managerRequest => managerRequest.Center)
            .OrderBy(managerRequest => managerRequest.Status == ModerationStatus.Pending ? 0 : 1)
            .ThenByDescending(managerRequest => managerRequest.CreatedAt)
            .Select(managerRequest => new AdminManagerRequestDto
            {
                Id = managerRequest.Id,
                UserId = managerRequest.UserId,
                UserName = managerRequest.User != null ? managerRequest.User.Name : string.Empty,
                UserEmail = managerRequest.User != null ? managerRequest.User.Email : string.Empty,
                CenterId = managerRequest.CenterId,
                CenterName = managerRequest.Center != null ? managerRequest.Center.Name : string.Empty,
                CenterCity = managerRequest.Center != null ? managerRequest.Center.City : string.Empty,
                CenterCountry = managerRequest.Center != null ? managerRequest.Center.Country : string.Empty,
                Status = managerRequest.Status,
                CreatedAt = managerRequest.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminManagerRequestDto>>.Success(requests);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminSuggestionDto>>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var suggestions = await dbContext.Suggestions
            .AsNoTracking()
            .Include(suggestion => suggestion.User)
            .OrderBy(suggestion => suggestion.Status == SuggestionReviewStatus.Pending ? 0 : 1)
            .ThenByDescending(suggestion => suggestion.CreatedAt)
            .Select(suggestion => new AdminSuggestionDto
            {
                Id = suggestion.Id,
                UserId = suggestion.UserId,
                UserName = suggestion.User != null ? suggestion.User.Name : string.Empty,
                UserEmail = suggestion.User != null ? suggestion.User.Email : string.Empty,
                Message = suggestion.Message,
                Type = suggestion.Type,
                Status = suggestion.Status,
                CreatedAt = suggestion.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminSuggestionDto>>.Success(suggestions);
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Email)
            .Select(user => new AdminUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                AssignedCenterCount = user.ManagedCenters.Count(centerManager => centerManager.Approved),
                CreatedAt = user.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminUserDto>>.Success(users);
    }

    public async Task<OperationResult> RejectCenterLanguageSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.CenterLanguageSuggestions
            .SingleOrDefaultAsync(currentSuggestion => currentSuggestion.Id == id, cancellationToken);

        if (suggestion is null)
        {
            return OperationResult.Failure("Center language suggestion not found.", 404);
        }

        if (suggestion.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending center language suggestions can be rejected.", 409);
        }

        suggestion.Status = ModerationStatus.Rejected;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminRejectedCenterLanguageSuggestion",
            entityName: nameof(CenterLanguageSuggestion),
            entityId: suggestion.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                suggestion.CenterId,
                suggestion.LanguageId,
                suggestion.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center language suggestion rejected.");
    }

    public async Task<OperationResult> RejectCenterRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var centerRequest = await dbContext.CenterRequests
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (centerRequest is null)
        {
            return OperationResult.Failure("Center request not found.", 404);
        }

        if (centerRequest.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending center requests can be rejected.", 409);
        }

        centerRequest.Status = ModerationStatus.Rejected;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminRejectedCenterRequest",
            entityName: nameof(CenterRequest),
            entityId: centerRequest.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                centerRequest.Name,
                centerRequest.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center request rejected.");
    }

    public async Task<OperationResult> RejectManagerRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var managerRequest = await dbContext.ManagerRequests
            .SingleOrDefaultAsync(currentRequest => currentRequest.Id == id, cancellationToken);

        if (managerRequest is null)
        {
            return OperationResult.Failure("Manager request not found.", 404);
        }

        if (managerRequest.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending manager requests can be rejected.", 409);
        }

        managerRequest.Status = ModerationStatus.Rejected;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminRejectedManagerRequest",
            entityName: nameof(ManagerRequest),
            entityId: managerRequest.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                managerRequest.UserId,
                managerRequest.CenterId,
                managerRequest.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Manager request rejected.");
    }

    public async Task<OperationResult> ReviewSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.Suggestions
            .SingleOrDefaultAsync(currentSuggestion => currentSuggestion.Id == id, cancellationToken);

        if (suggestion is null)
        {
            return OperationResult.Failure("Suggestion not found.", 404);
        }

        if (suggestion.Status == SuggestionReviewStatus.Reviewed)
        {
            return OperationResult.Failure("Suggestion has already been reviewed.", 409);
        }

        suggestion.Status = SuggestionReviewStatus.Reviewed;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminReviewedSuggestion",
            entityName: nameof(Suggestion),
            entityId: suggestion.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                suggestion.Type,
                suggestion.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Suggestion marked as reviewed.");
    }

    public async Task<OperationResult> UpdateCenterAsync(int id, UpdateCenterDto request, int adminUserId, CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers.SingleOrDefaultAsync(currentCenter => currentCenter.Id == id, cancellationToken);
        if (center is null)
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        if (await SimilarCenterExistsAsync(
                id,
                request.Name,
                request.City,
                request.Country,
                request.Latitude,
                request.Longitude,
                cancellationToken))
        {
            return OperationResult.Failure("A similar center already exists in NoorLocator.", 409);
        }

        center.Name = request.Name.Trim();
        center.Address = request.Address.Trim();
        center.City = request.City.Trim();
        center.Country = request.Country.Trim();
        center.Latitude = request.Latitude;
        center.Longitude = request.Longitude;
        center.Description = request.Description.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminUpdatedCenter",
            entityName: nameof(Center),
            entityId: center.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                center.Name,
                center.City,
                center.Country,
                center.Latitude,
                center.Longitude
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Center updated successfully.");
    }

    private async Task<bool> SimilarCenterExistsAsync(
        int currentEntityId,
        string name,
        string city,
        string country,
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var normalizedCity = city.Trim().ToLowerInvariant();
        var normalizedCountry = country.Trim().ToLowerInvariant();
        var minLatitude = latitude - 0.02m;
        var maxLatitude = latitude + 0.02m;
        var minLongitude = longitude - 0.02m;
        var maxLongitude = longitude + 0.02m;

        return await dbContext.Centers.AnyAsync(
            center =>
                center.Id != currentEntityId &&
                center.Name.ToLower() == normalizedName &&
                ((center.City.ToLower() == normalizedCity && center.Country.ToLower() == normalizedCountry) ||
                 (center.Latitude >= minLatitude && center.Latitude <= maxLatitude &&
                  center.Longitude >= minLongitude && center.Longitude <= maxLongitude)),
            cancellationToken);
    }
}
