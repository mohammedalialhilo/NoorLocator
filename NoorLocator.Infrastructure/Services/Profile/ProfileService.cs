using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Notifications.Dtos;
using NoorLocator.Application.Profile.Dtos;
using NoorLocator.Application.Profile.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Email;

namespace NoorLocator.Infrastructure.Services.Profile;

public class ProfileService(
    NoorLocatorDbContext dbContext,
    AuditLogger auditLogger,
    PasswordHashingService passwordHashingService,
    INoorLocatorEmailService emailService,
    IOptions<AuthFlowSettings> authFlowOptions) : IProfileService
{
    private readonly AuthFlowSettings authFlowSettings = authFlowOptions.Value;

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
        var emailChanged = !string.Equals(previousEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase);

        user.Name = normalizedName;
        user.Email = normalizedEmail;
        user.UpdatedAtUtc = DateTime.UtcNow;

        string? verificationToken = null;
        if (emailChanged)
        {
            verificationToken = CreateSecureToken();
            user.IsEmailVerified = false;
            user.EmailVerificationTokenHash = passwordHashingService.HashToken(verificationToken);
            user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(authFlowSettings.EmailVerificationTokenLifetimeMinutes);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(verificationToken))
        {
            await emailService.SendVerificationEmailAsync(user, verificationToken, cancellationToken);
        }

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
                user.IsEmailVerified,
                Role = user.Role.ToString()
            },
            cancellationToken);

        return OperationResult<CurrentUserDto>.Success(
            MapCurrentUser(user),
            emailChanged
                ? "Profile updated. Please verify your new email address."
                : "Profile updated successfully.");
    }

    public async Task<OperationResult<NotificationPreferenceDto>> GetNotificationPreferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return OperationResult<NotificationPreferenceDto>.Failure("Authenticated user was not found.", 404);
        }

        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        return OperationResult<NotificationPreferenceDto>.Success(MapPreference(preference));
    }

    public async Task<OperationResult<NotificationPreferenceDto>> UpdateNotificationPreferencesAsync(int userId, UpdateNotificationPreferencesDto request, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return OperationResult<NotificationPreferenceDto>.Failure("Authenticated user was not found.", 404);
        }

        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        preference.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        preference.AppNotificationsEnabled = request.AppNotificationsEnabled;
        preference.MajlisNotificationsEnabled = request.MajlisNotificationsEnabled;
        preference.EventNotificationsEnabled = request.EventNotificationsEnabled;
        preference.CenterUpdatesEnabled = request.CenterUpdatesEnabled;
        preference.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "profile.notification-preferences.updated",
            entityName: nameof(UserNotificationPreference),
            entityId: preference.UserId.ToString(),
            userId: userId,
            metadata: request,
            cancellationToken);

        return OperationResult<NotificationPreferenceDto>.Success(
            MapPreference(preference),
            "Notification settings updated successfully.");
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

    private async Task<UserNotificationPreference> GetOrCreatePreferenceAsync(int userId, CancellationToken cancellationToken)
    {
        var preference = await dbContext.UserNotificationPreferences
            .SingleOrDefaultAsync(currentPreference => currentPreference.UserId == userId, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new UserNotificationPreference
        {
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.UserNotificationPreferences.Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken);
        return preference;
    }

    private static string CreateSecureToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static CurrentUserDto MapCurrentUser(User user)
    {
        return new CurrentUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAtUtc = user.LastLoginAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            AssignedCenterIds = user.ManagedCenters
                .Where(centerManager => centerManager.Approved)
                .Select(centerManager => centerManager.CenterId)
                .Distinct()
                .ToArray()
        };
    }

    private static NotificationPreferenceDto MapPreference(UserNotificationPreference preference)
    {
        return new NotificationPreferenceDto
        {
            EmailNotificationsEnabled = preference.EmailNotificationsEnabled,
            AppNotificationsEnabled = preference.AppNotificationsEnabled,
            MajlisNotificationsEnabled = preference.MajlisNotificationsEnabled,
            EventNotificationsEnabled = preference.EventNotificationsEnabled,
            CenterUpdatesEnabled = preference.CenterUpdatesEnabled
        };
    }
}
