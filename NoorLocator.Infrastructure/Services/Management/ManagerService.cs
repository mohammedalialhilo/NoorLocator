using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Management;

public class ManagerService(NoorLocatorDbContext dbContext) : IManagerService
{
    public async Task<OperationResult> ApproveCenterLanguageSuggestionAsync(ApproveLanguageSuggestionDto request, CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.CenterLanguageSuggestions
            .SingleOrDefaultAsync(currentSuggestion => currentSuggestion.Id == request.SuggestionId, cancellationToken);

        if (suggestion is null)
        {
            return OperationResult.Failure("Language suggestion not found.", 404);
        }

        if (suggestion.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending suggestions can be approved.", 409);
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

        return OperationResult.Success("Language suggestion approved.");
    }

    public async Task<OperationResult> ApproveManagerAsync(ApproveManagerRequestDto request, CancellationToken cancellationToken = default)
    {
        var managerRequest = await dbContext.ManagerRequests
            .Include(currentRequest => currentRequest.User)
            .SingleOrDefaultAsync(currentRequest => currentRequest.Id == request.ManagerRequestId, cancellationToken);

        if (managerRequest is null)
        {
            return OperationResult.Failure("Manager request not found.", 404);
        }

        if (managerRequest.Status != ModerationStatus.Pending)
        {
            return OperationResult.Failure("Only pending manager requests can be approved.", 409);
        }

        var assignment = await dbContext.CenterManagers.SingleOrDefaultAsync(
            centerManager => centerManager.UserId == managerRequest.UserId && centerManager.CenterId == managerRequest.CenterId,
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
        return OperationResult.Success("Manager request approved.");
    }

    public async Task<OperationResult> RequestManagerAccessAsync(ManagerRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        var centerExists = await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken);
        if (!centerExists)
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        var alreadyAssigned = await dbContext.CenterManagers.AnyAsync(
            centerManager => centerManager.UserId == userId &&
                             centerManager.CenterId == request.CenterId &&
                             centerManager.Approved,
            cancellationToken);

        if (alreadyAssigned)
        {
            return OperationResult.Failure("This user is already an approved manager for the selected center.", 409);
        }

        var pendingRequestExists = await dbContext.ManagerRequests.AnyAsync(
            managerRequest => managerRequest.UserId == userId &&
                              managerRequest.CenterId == request.CenterId &&
                              managerRequest.Status == ModerationStatus.Pending,
            cancellationToken);

        if (pendingRequestExists)
        {
            return OperationResult.Failure("A pending manager request already exists for this center.", 409);
        }

        dbContext.ManagerRequests.Add(new ManagerRequest
        {
            UserId = userId,
            CenterId = request.CenterId,
            Status = ModerationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Accepted("Manager request submitted for admin review.");
    }

    public async Task<OperationResult> SuggestCenterLanguageAsync(CreateCenterLanguageSuggestionDto request, int userId, CancellationToken cancellationToken = default)
    {
        var centerExists = await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken);
        if (!centerExists)
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        var languageExists = await dbContext.Languages.AnyAsync(language => language.Id == request.LanguageId, cancellationToken);
        if (!languageExists)
        {
            return OperationResult.Failure("Language must come from the predefined language table.", 400);
        }

        var alreadyAssigned = await dbContext.CenterLanguages.AnyAsync(
            centerLanguage => centerLanguage.CenterId == request.CenterId && centerLanguage.LanguageId == request.LanguageId,
            cancellationToken);

        if (alreadyAssigned)
        {
            return OperationResult.Failure("That language is already associated with the center.", 409);
        }

        var pendingSuggestionExists = await dbContext.CenterLanguageSuggestions.AnyAsync(
            suggestion => suggestion.CenterId == request.CenterId &&
                          suggestion.LanguageId == request.LanguageId &&
                          suggestion.SuggestedByUserId == userId &&
                          suggestion.Status == ModerationStatus.Pending,
            cancellationToken);

        if (pendingSuggestionExists)
        {
            return OperationResult.Failure("A pending suggestion for this language already exists.", 409);
        }

        dbContext.CenterLanguageSuggestions.Add(new CenterLanguageSuggestion
        {
            CenterId = request.CenterId,
            LanguageId = request.LanguageId,
            SuggestedByUserId = userId,
            Status = ModerationStatus.Pending
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Accepted("Language suggestion submitted for admin review.");
    }
}
