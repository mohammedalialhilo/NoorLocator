using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Common.Localization;
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
    public async Task<OperationResult<AdminManagerAssignmentDto>> CreateManagerAssignmentAsync(CreateAdminManagerAssignmentDto request, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(currentUser => currentUser.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("User not found.", 404);
        }

        if (user.Role == UserRole.Admin)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Admin accounts do not need manager-center assignments.", 409);
        }

        var center = await dbContext.Centers.SingleOrDefaultAsync(currentCenter => currentCenter.Id == request.CenterId, cancellationToken);
        if (center is null)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Center not found.", 404);
        }

        var existingAssignment = await dbContext.CenterManagers.SingleOrDefaultAsync(
            centerManager => centerManager.UserId == request.UserId && centerManager.CenterId == request.CenterId,
            cancellationToken);

        if (existingAssignment is not null && existingAssignment.Approved)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("This manager assignment already exists.", 409);
        }

        CenterManager assignment;
        if (existingAssignment is null)
        {
            assignment = new CenterManager
            {
                UserId = request.UserId,
                CenterId = request.CenterId,
                Approved = true
            };
            dbContext.CenterManagers.Add(assignment);
        }
        else
        {
            assignment = existingAssignment;
            assignment.Approved = true;
        }

        if (user.Role == UserRole.User)
        {
            user.Role = UserRole.Manager;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminCreatedManagerAssignment",
            entityName: nameof(CenterManager),
            entityId: assignment.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                assignment.UserId,
                assignment.CenterId,
                assignment.Approved
            },
            cancellationToken: cancellationToken);

        var payload = await GetManagerAssignmentDtoAsync(assignment.Id, cancellationToken);
        return OperationResult<AdminManagerAssignmentDto>.Success(payload, "Manager assignment created successfully.");
    }

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

    public async Task<OperationResult<AdminManagerAssignmentDto>> UpdateManagerAssignmentAsync(int id, UpdateAdminManagerAssignmentDto request, int adminUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.CenterManagers
            .Include(centerManager => centerManager.User)
            .SingleOrDefaultAsync(centerManager => centerManager.Id == id, cancellationToken);

        if (assignment is null)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Manager assignment not found.", 404);
        }

        var targetUser = await dbContext.Users.SingleOrDefaultAsync(currentUser => currentUser.Id == request.UserId, cancellationToken);
        if (targetUser is null)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("User not found.", 404);
        }

        if (targetUser.Role == UserRole.Admin)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Admin accounts do not need manager-center assignments.", 409);
        }

        var centerExists = await dbContext.Centers.AnyAsync(currentCenter => currentCenter.Id == request.CenterId, cancellationToken);
        if (!centerExists)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Center not found.", 404);
        }

        var duplicateExists = await dbContext.CenterManagers.AnyAsync(
            centerManager => centerManager.Id != id &&
                             centerManager.UserId == request.UserId &&
                             centerManager.CenterId == request.CenterId,
            cancellationToken);

        if (duplicateExists)
        {
            return OperationResult<AdminManagerAssignmentDto>.Failure("Another assignment already links this user to the selected center.", 409);
        }

        var previousUserId = assignment.UserId;
        var previousCenterId = assignment.CenterId;

        assignment.UserId = request.UserId;
        assignment.CenterId = request.CenterId;
        assignment.Approved = true;

        if (targetUser.Role == UserRole.User)
        {
            targetUser.Role = UserRole.Manager;
            targetUser.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await DowngradeManagerIfUnassignedAsync(previousUserId, cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminUpdatedManagerAssignment",
            entityName: nameof(CenterManager),
            entityId: assignment.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                PreviousUserId = previousUserId,
                PreviousCenterId = previousCenterId,
                assignment.UserId,
                assignment.CenterId
            },
            cancellationToken: cancellationToken);

        var payload = await GetManagerAssignmentDtoAsync(assignment.Id, cancellationToken);
        return OperationResult<AdminManagerAssignmentDto>.Success(payload, "Manager assignment updated successfully.");
    }

    public async Task<OperationResult> DeleteManagerAssignmentAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.CenterManagers.SingleOrDefaultAsync(centerManager => centerManager.Id == id, cancellationToken);
        if (assignment is null)
        {
            return OperationResult.Failure("Manager assignment not found.", 404);
        }

        var userId = assignment.UserId;
        dbContext.CenterManagers.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DowngradeManagerIfUnassignedAsync(userId, cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminDeletedManagerAssignment",
            entityName: nameof(CenterManager),
            entityId: id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                assignment.UserId,
                assignment.CenterId
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("Manager assignment removed successfully.");
    }

    public async Task<OperationResult> DeleteUserAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(currentUser => currentUser.CenterRequests)
            .Include(currentUser => currentUser.ManagedCenters)
            .Include(currentUser => currentUser.ManagerRequests)
            .Include(currentUser => currentUser.CenterLanguageSuggestions)
            .Include(currentUser => currentUser.Suggestions)
            .Include(currentUser => currentUser.CreatedMajalis)
            .Include(currentUser => currentUser.EventAnnouncements)
            .Include(currentUser => currentUser.UploadedCenterImages)
            .Include(currentUser => currentUser.AuditLogs)
            .SingleOrDefaultAsync(currentUser => currentUser.Id == id, cancellationToken);

        if (user is null)
        {
            return OperationResult.Failure("User not found.", 404);
        }

        var adminCount = await dbContext.Users.CountAsync(currentUser => currentUser.Role == UserRole.Admin, cancellationToken);
        var deleteGuard = BuildDeleteGuard(
            isSelf: user.Id == adminUserId,
            isLastAdmin: user.Role == UserRole.Admin && adminCount <= 1,
            hasMajalis: user.CreatedMajalis.Count > 0,
            hasAnnouncements: user.EventAnnouncements.Count > 0,
            hasImages: user.UploadedCenterImages.Count > 0,
            hasAuditLogs: user.AuditLogs.Count > 0);

        if (!deleteGuard.canDelete)
        {
            return OperationResult.Failure(deleteGuard.reason, 409);
        }

        if (user.CenterRequests.Count > 0)
        {
            dbContext.CenterRequests.RemoveRange(user.CenterRequests);
        }

        if (user.ManagedCenters.Count > 0)
        {
            dbContext.CenterManagers.RemoveRange(user.ManagedCenters);
        }

        if (user.ManagerRequests.Count > 0)
        {
            dbContext.ManagerRequests.RemoveRange(user.ManagerRequests);
        }

        if (user.CenterLanguageSuggestions.Count > 0)
        {
            dbContext.CenterLanguageSuggestions.RemoveRange(user.CenterLanguageSuggestions);
        }

        if (user.Suggestions.Count > 0)
        {
            dbContext.Suggestions.RemoveRange(user.Suggestions);
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminDeletedUser",
            entityName: nameof(User),
            entityId: id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                user.Name,
                user.Email,
                Role = user.Role.ToString()
            },
            cancellationToken: cancellationToken);

        return OperationResult.Success("User deleted successfully.");
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

    public async Task<OperationResult<IReadOnlyCollection<AdminManagerAssignmentDto>>> GetManagerAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await dbContext.CenterManagers
            .AsNoTracking()
            .Where(centerManager => centerManager.Approved)
            .Include(centerManager => centerManager.User)
            .Include(centerManager => centerManager.Center)
            .OrderBy(centerManager => centerManager.User!.Name)
            .ThenBy(centerManager => centerManager.Center!.Country)
            .ThenBy(centerManager => centerManager.Center!.City)
            .ThenBy(centerManager => centerManager.Center!.Name)
            .Select(centerManager => new AdminManagerAssignmentDto
            {
                Id = centerManager.Id,
                UserId = centerManager.UserId,
                UserName = centerManager.User != null ? centerManager.User.Name : string.Empty,
                UserEmail = centerManager.User != null ? centerManager.User.Email : string.Empty,
                UserRole = centerManager.User != null ? centerManager.User.Role.ToString() : UserRole.User.ToString(),
                CenterId = centerManager.CenterId,
                CenterName = centerManager.Center != null ? centerManager.Center.Name : string.Empty,
                CenterCity = centerManager.Center != null ? centerManager.Center.City : string.Empty,
                CenterCountry = centerManager.Center != null ? centerManager.Center.Country : string.Empty,
                Approved = centerManager.Approved,
                MajlisCount = dbContext.Majalis.Count(majlis => majlis.CenterId == centerManager.CenterId && majlis.CreatedByManagerId == centerManager.UserId),
                AnnouncementCount = dbContext.EventAnnouncements.Count(announcement => announcement.CenterId == centerManager.CenterId && announcement.CreatedByManagerId == centerManager.UserId)
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<AdminManagerAssignmentDto>>.Success(assignments);
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

    public async Task<OperationResult<AdminUserDetailsDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(currentUser => currentUser.NotificationPreference)
            .Include(currentUser => currentUser.ManagedCenters)
                .ThenInclude(centerManager => centerManager.Center)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.Center)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.MajlisLanguages)
                    .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .Include(currentUser => currentUser.EventAnnouncements)
                .ThenInclude(announcement => announcement.Center)
            .Include(currentUser => currentUser.UploadedCenterImages)
            .Include(currentUser => currentUser.AuditLogs)
            .SingleOrDefaultAsync(currentUser => currentUser.Id == id, cancellationToken);

        if (user is null)
        {
            return OperationResult<AdminUserDetailsDto>.Failure("User not found.", 404);
        }

        var adminCount = await dbContext.Users.CountAsync(currentUser => currentUser.Role == UserRole.Admin, cancellationToken);
        return OperationResult<AdminUserDetailsDto>.Success(MapAdminUserDetails(user, adminCount, isSelf: false));
    }

    public async Task<OperationResult<IReadOnlyCollection<AdminUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var adminCount = await dbContext.Users.CountAsync(user => user.Role == UserRole.Admin, cancellationToken);

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.IsEmailVerified,
                user.PreferredLanguageCode,
                AssignedCenterCount = user.ManagedCenters.Count(centerManager => centerManager.Approved),
                user.LastLoginAtUtc,
                user.CreatedAt,
                HasMajalis = user.CreatedMajalis.Any(),
                HasAnnouncements = user.EventAnnouncements.Any(),
                HasImages = user.UploadedCenterImages.Any(),
                HasAuditLogs = user.AuditLogs.Any()
            })
            .ToArrayAsync(cancellationToken);

        var payload = users
            .Select(user =>
            {
                var deleteGuard = BuildDeleteGuard(
                    isSelf: false,
                    isLastAdmin: user.Role == UserRole.Admin && adminCount <= 1,
                    hasMajalis: user.HasMajalis,
                    hasAnnouncements: user.HasAnnouncements,
                    hasImages: user.HasImages,
                    hasAuditLogs: user.HasAuditLogs);

                return new AdminUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    PreferredLanguageCode = SupportedLanguageCatalog.NormalizeOrFallback(user.PreferredLanguageCode),
                    AssignedCenterCount = user.AssignedCenterCount,
                    LastLoginAtUtc = user.LastLoginAtUtc,
                    CanDelete = deleteGuard.canDelete,
                    DeleteBlockedReason = deleteGuard.reason,
                    CreatedAt = user.CreatedAt
                };
            })
            .ToArray();

        return OperationResult<IReadOnlyCollection<AdminUserDto>>.Success(payload);
    }

    public async Task<OperationResult<AdminUserDetailsDto>> UpdateUserAsync(int id, UpdateAdminUserDto request, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(currentUser => currentUser.ManagedCenters)
            .Include(currentUser => currentUser.NotificationPreference)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.Center)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.MajlisLanguages)
                    .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .Include(currentUser => currentUser.EventAnnouncements)
                .ThenInclude(announcement => announcement.Center)
            .Include(currentUser => currentUser.UploadedCenterImages)
            .Include(currentUser => currentUser.AuditLogs)
            .SingleOrDefaultAsync(currentUser => currentUser.Id == id, cancellationToken);

        if (user is null)
        {
            return OperationResult<AdminUserDetailsDto>.Failure("User not found.", 404);
        }

        if (user.Id == adminUserId && request.Role != UserRole.Admin)
        {
            return OperationResult<AdminUserDetailsDto>.Failure("You cannot remove your own admin access from the active session.", 409);
        }

        var adminCount = await dbContext.Users.CountAsync(currentUser => currentUser.Role == UserRole.Admin, cancellationToken);
        if (user.Role == UserRole.Admin && request.Role != UserRole.Admin && adminCount <= 1)
        {
            return OperationResult<AdminUserDetailsDto>.Failure("At least one admin account must remain active.", 409);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedName = request.Name.Trim();

        var duplicateEmailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(currentUser => currentUser.Id != id && currentUser.Email == normalizedEmail, cancellationToken);

        if (duplicateEmailExists)
        {
            return OperationResult<AdminUserDetailsDto>.Failure("An account with this email already exists.", 409);
        }

        var previousName = user.Name;
        var previousEmail = user.Email;
        var previousRole = user.Role;
        var previousLanguage = user.PreferredLanguageCode;
        var removedAssignments = Array.Empty<int>();

        user.Name = normalizedName;
        user.Email = normalizedEmail;
        user.PreferredLanguageCode = SupportedLanguageCatalog.NormalizeOrFallback(request.PreferredLanguageCode);
        user.Role = request.Role;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Role == UserRole.User && user.ManagedCenters.Count > 0)
        {
            removedAssignments = user.ManagedCenters
                .Where(centerManager => centerManager.Approved)
                .Select(centerManager => centerManager.CenterId)
                .Distinct()
                .ToArray();

            dbContext.CenterManagers.RemoveRange(user.ManagedCenters);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "AdminUpdatedUser",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: adminUserId,
            metadata: new
            {
                PreviousName = previousName,
                UpdatedName = user.Name,
                PreviousEmail = previousEmail,
                UpdatedEmail = user.Email,
                PreviousRole = previousRole.ToString(),
                UpdatedRole = user.Role.ToString(),
                PreviousPreferredLanguageCode = previousLanguage,
                UpdatedPreferredLanguageCode = user.PreferredLanguageCode,
                RemovedAssignmentCenterIds = removedAssignments
            },
            cancellationToken: cancellationToken);

        var refreshedUser = await dbContext.Users
            .AsNoTracking()
            .Include(currentUser => currentUser.NotificationPreference)
            .Include(currentUser => currentUser.ManagedCenters)
                .ThenInclude(centerManager => centerManager.Center)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.Center)
            .Include(currentUser => currentUser.CreatedMajalis)
                .ThenInclude(majlis => majlis.MajlisLanguages)
                    .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .Include(currentUser => currentUser.EventAnnouncements)
                .ThenInclude(announcement => announcement.Center)
            .Include(currentUser => currentUser.UploadedCenterImages)
            .Include(currentUser => currentUser.AuditLogs)
            .SingleAsync(currentUser => currentUser.Id == id, cancellationToken);

        return OperationResult<AdminUserDetailsDto>.Success(
            MapAdminUserDetails(refreshedUser, adminCount, isSelf: refreshedUser.Id == adminUserId),
            "User updated successfully.");
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

    private static (bool canDelete, string reason) BuildDeleteGuard(bool isSelf, bool isLastAdmin, bool hasMajalis, bool hasAnnouncements, bool hasImages, bool hasAuditLogs)
    {
        if (isSelf)
        {
            return (false, "You cannot delete your own active admin account.");
        }

        if (isLastAdmin)
        {
            return (false, "At least one admin account must remain active.");
        }

        if (hasMajalis)
        {
            return (false, "This user cannot be deleted because they own majalis records. Reassign or delete those majalis first.");
        }

        if (hasAnnouncements)
        {
            return (false, "This user cannot be deleted because they own event announcements. Reassign or delete those announcements first.");
        }

        if (hasImages)
        {
            return (false, "This user cannot be deleted because they uploaded center gallery images. Remove those images first.");
        }

        if (hasAuditLogs)
        {
            return (false, "This user cannot be deleted because their audit history must remain intact.");
        }

        return (true, string.Empty);
    }

    private async Task DowngradeManagerIfUnassignedAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);
        if (user is null || user.Role != UserRole.Manager)
        {
            return;
        }

        var hasApprovedAssignment = await dbContext.CenterManagers.AnyAsync(
            centerManager => centerManager.UserId == userId && centerManager.Approved,
            cancellationToken);

        if (hasApprovedAssignment)
        {
            return;
        }

        user.Role = UserRole.User;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminManagerAssignmentDto> GetManagerAssignmentDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.CenterManagers
            .AsNoTracking()
            .Where(centerManager => centerManager.Id == id)
            .Include(centerManager => centerManager.User)
            .Include(centerManager => centerManager.Center)
            .Select(centerManager => new AdminManagerAssignmentDto
            {
                Id = centerManager.Id,
                UserId = centerManager.UserId,
                UserName = centerManager.User != null ? centerManager.User.Name : string.Empty,
                UserEmail = centerManager.User != null ? centerManager.User.Email : string.Empty,
                UserRole = centerManager.User != null ? centerManager.User.Role.ToString() : UserRole.User.ToString(),
                CenterId = centerManager.CenterId,
                CenterName = centerManager.Center != null ? centerManager.Center.Name : string.Empty,
                CenterCity = centerManager.Center != null ? centerManager.Center.City : string.Empty,
                CenterCountry = centerManager.Center != null ? centerManager.Center.Country : string.Empty,
                Approved = centerManager.Approved,
                MajlisCount = dbContext.Majalis.Count(majlis => majlis.CenterId == centerManager.CenterId && majlis.CreatedByManagerId == centerManager.UserId),
                AnnouncementCount = dbContext.EventAnnouncements.Count(announcement => announcement.CenterId == centerManager.CenterId && announcement.CreatedByManagerId == centerManager.UserId)
            })
            .SingleAsync(cancellationToken);
    }

    private static AdminUserDetailsDto MapAdminUserDetails(User user, int adminCount, bool isSelf)
    {
        var deleteGuard = BuildDeleteGuard(
            isSelf,
            isLastAdmin: user.Role == UserRole.Admin && adminCount <= 1,
            hasMajalis: user.CreatedMajalis.Count > 0,
            hasAnnouncements: user.EventAnnouncements.Count > 0,
            hasImages: user.UploadedCenterImages.Count > 0,
            hasAuditLogs: user.AuditLogs.Count > 0);

        return new AdminUserDetailsDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            PreferredLanguageCode = SupportedLanguageCatalog.NormalizeOrFallback(user.PreferredLanguageCode),
            CreatedAt = user.CreatedAt,
            LastLoginAtUtc = user.LastLoginAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            AssignedCenterCount = user.ManagedCenters.Count(centerManager => centerManager.Approved),
            CanDelete = deleteGuard.canDelete,
            DeleteBlockedReason = deleteGuard.reason,
            NotificationPreference = new AdminUserNotificationPreferenceDto
            {
                EmailNotificationsEnabled = user.NotificationPreference?.EmailNotificationsEnabled ?? true,
                AppNotificationsEnabled = user.NotificationPreference?.AppNotificationsEnabled ?? true,
                MajlisNotificationsEnabled = user.NotificationPreference?.MajlisNotificationsEnabled ?? true,
                EventNotificationsEnabled = user.NotificationPreference?.EventNotificationsEnabled ?? true,
                CenterUpdatesEnabled = user.NotificationPreference?.CenterUpdatesEnabled ?? true
            },
            ManagedCenters = user.ManagedCenters
                .Where(centerManager => centerManager.Approved)
                .OrderBy(centerManager => centerManager.Center!.Country)
                .ThenBy(centerManager => centerManager.Center!.City)
                .ThenBy(centerManager => centerManager.Center!.Name)
                .Select(centerManager => new AdminManagedCenterDto
                {
                    AssignmentId = centerManager.Id,
                    CenterId = centerManager.CenterId,
                    CenterName = centerManager.Center?.Name ?? string.Empty,
                    CenterCity = centerManager.Center?.City ?? string.Empty,
                    CenterCountry = centerManager.Center?.Country ?? string.Empty,
                    Approved = centerManager.Approved
                })
                .ToArray(),
            CreatedMajalis = user.CreatedMajalis
                .OrderByDescending(majlis => majlis.CreatedAt)
                .Select(majlis => new AdminManagedMajlisDto
                {
                    Id = majlis.Id,
                    Title = majlis.Title,
                    Description = majlis.Description,
                    Date = majlis.Date,
                    Time = majlis.Time,
                    CenterId = majlis.CenterId,
                    CenterName = majlis.Center?.Name ?? string.Empty,
                    CenterCity = majlis.Center?.City ?? string.Empty,
                    CenterCountry = majlis.Center?.Country ?? string.Empty,
                    Languages = majlis.MajlisLanguages
                        .Where(majlisLanguage => majlisLanguage.Language != null)
                        .OrderBy(majlisLanguage => majlisLanguage.Language!.Code)
                        .Select(majlisLanguage => new AdminLanguageOptionDto
                        {
                            Id = majlisLanguage.LanguageId,
                            Code = majlisLanguage.Language!.Code,
                            Name = majlisLanguage.Language.Name
                        })
                        .ToArray()
                })
                .ToArray(),
            CreatedAnnouncements = user.EventAnnouncements
                .OrderByDescending(announcement => announcement.CreatedAt)
                .Select(announcement => new AdminManagedAnnouncementDto
                {
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Description = announcement.Description,
                    Status = announcement.Status.ToString(),
                    CenterId = announcement.CenterId,
                    CenterName = announcement.Center?.Name ?? string.Empty,
                    CenterCity = announcement.Center?.City ?? string.Empty,
                    CenterCountry = announcement.Center?.Country ?? string.Empty,
                    CreatedAt = announcement.CreatedAt
                })
                .ToArray()
        };
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
