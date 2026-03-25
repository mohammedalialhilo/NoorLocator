using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Profile.Dtos;
using NoorLocator.Application.Profile.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.Infrastructure.Services.Profile;

public class ProfileService(
    NoorLocatorDbContext dbContext,
    AuditLogger auditLogger) : IProfileService
{
    public async Task<OperationResult<CurrentUserDto>> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, asNoTracking: true, cancellationToken);
        if (user is null)
        {
            return OperationResult<CurrentUserDto>.Failure("Authenticated user was not found.", 404);
        }

        return OperationResult<CurrentUserDto>.Success(MapCurrentUser(user));
    }

    public async Task<OperationResult<CurrentUserDto>> UpdateMyProfileAsync(int userId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, asNoTracking: false, cancellationToken);
        if (user is null)
        {
            return OperationResult<CurrentUserDto>.Failure("Authenticated user was not found.", 404);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedName = request.Name.Trim();

        var duplicateEmailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                currentUser => currentUser.Id != userId && currentUser.Email == normalizedEmail,
                cancellationToken);

        if (duplicateEmailExists)
        {
            return OperationResult<CurrentUserDto>.Failure("An account with this email already exists.", 409);
        }

        var previousName = user.Name;
        var previousEmail = user.Email;

        user.Name = normalizedName;
        user.Email = normalizedEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "profile.updated",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new
            {
                PreviousName = previousName,
                UpdatedName = user.Name,
                PreviousEmail = previousEmail,
                UpdatedEmail = user.Email,
                Role = user.Role.ToString()
            },
            cancellationToken);

        return OperationResult<CurrentUserDto>.Success(
            MapCurrentUser(user),
            "Profile updated successfully.");
    }

    private async Task<User?> LoadUserAsync(int userId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .Include(currentUser => currentUser.ManagedCenters.Where(centerManager => centerManager.Approved))
            .Where(currentUser => currentUser.Id == userId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private static CurrentUserDto MapCurrentUser(User user)
    {
        return new CurrentUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            AssignedCenterIds = user.ManagedCenters
                .Where(centerManager => centerManager.Approved)
                .Select(centerManager => centerManager.CenterId)
                .Distinct()
                .ToArray()
        };
    }
}
