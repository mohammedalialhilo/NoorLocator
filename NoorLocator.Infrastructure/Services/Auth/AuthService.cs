using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Email;

namespace NoorLocator.Infrastructure.Services.Auth;

public class AuthService(
    NoorLocatorDbContext dbContext,
    PasswordHashingService passwordHashingService,
    JwtTokenFactory jwtTokenFactory,
    AuditLogger auditLogger,
    INoorLocatorEmailService emailService,
    IOptions<AuthFlowSettings> authFlowOptions) : IAuthService
{
    private readonly AuthFlowSettings authFlowSettings = authFlowOptions.Value;

    public async Task<OperationResult<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, asNoTracking: true, cancellationToken);
        if (user is null)
        {
            return OperationResult<CurrentUserDto>.Failure("Authenticated user was not found.", 404);
        }

        return OperationResult<CurrentUserDto>.Success(MapCurrentUser(user));
    }

    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await LoadUserByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await auditLogger.WriteAsync(
                action: "auth.login.failed",
                entityName: nameof(User),
                entityId: null,
                userId: user?.Id,
                metadata: new { email },
                cancellationToken);

            return OperationResult<AuthResponseDto>.Failure("Invalid email or password.", 401);
        }

        if (!user.IsEmailVerified)
        {
            await auditLogger.WriteAsync(
                action: "auth.login.blocked.unverified",
                entityName: nameof(User),
                entityId: user.Id.ToString(),
                userId: user.Id,
                metadata: new { user.Email },
                cancellationToken);

            return OperationResult<AuthResponseDto>.Failure("Please verify your email before signing in.", 403);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var response = await BuildAuthResponseAsync(user, "Login successful.", cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.login.succeeded",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email, Role = user.Role.ToString() },
            cancellationToken);

        return OperationResult<AuthResponseDto>.Success(response, "Login successful.");
    }

    public async Task<OperationResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            await auditLogger.WriteAsync(
                action: "auth.register.duplicate",
                entityName: nameof(User),
                entityId: null,
                userId: null,
                metadata: new { email },
                cancellationToken);

            return OperationResult<AuthResponseDto>.Failure("An account with this email already exists.", 409);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHashingService.HashPassword(request.Password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = false
        };

        var verificationToken = CreateSecureToken();
        user.EmailVerificationTokenHash = passwordHashingService.HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(authFlowSettings.EmailVerificationTokenLifetimeMinutes);

        dbContext.Users.Add(user);
        dbContext.UserNotificationPreferences.Add(new UserNotificationPreference
        {
            User = user,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await emailService.SendVerificationEmailAsync(user, verificationToken, cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.register.succeeded",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email, Role = user.Role.ToString() },
            cancellationToken);

        var response = new AuthResponseDto
        {
            Role = user.Role.ToString(),
            Message = "Please check your email to verify your account.",
            User = MapCurrentUser(user)
        };

        return OperationResult<AuthResponseDto>.Success(response, "Please check your email to verify your account.", 201);
    }

    public async Task<OperationResult<VerifyEmailResultDto>> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return OperationResult<VerifyEmailResultDto>.Failure(
                "This verification link is invalid.",
                400,
                new VerifyEmailResultDto { Status = "invalid" });
        }

        var tokenHash = passwordHashingService.HashToken(token.Trim());
        var user = await dbContext.Users.SingleOrDefaultAsync(
            currentUser => currentUser.EmailVerificationTokenHash == tokenHash,
            cancellationToken);

        if (user is null)
        {
            return OperationResult<VerifyEmailResultDto>.Failure(
                "This verification link is invalid.",
                400,
                new VerifyEmailResultDto { Status = "invalid" });
        }

        if (!user.EmailVerificationTokenExpiresAtUtc.HasValue || user.EmailVerificationTokenExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiresAtUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return OperationResult<VerifyEmailResultDto>.Failure(
                "This verification link has expired. Request a new verification email.",
                410,
                new VerifyEmailResultDto
                {
                    Status = "expired",
                    Email = user.Email
                });
        }

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.email.verified",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email },
            cancellationToken);

        return OperationResult<VerifyEmailResultDto>.Success(
            new VerifyEmailResultDto
            {
                Status = "verified",
                Email = user.Email
            },
            "Your email has been verified.");
    }

    public async Task<OperationResult> ResendVerificationEmailAsync(int? currentUserId, string? email, CancellationToken cancellationToken = default)
    {
        User? user = null;
        if (currentUserId.HasValue)
        {
            user = await dbContext.Users.SingleOrDefaultAsync(current => current.Id == currentUserId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            user = await dbContext.Users.SingleOrDefaultAsync(current => current.Email == normalizedEmail, cancellationToken);
        }

        if (user is null || user.IsEmailVerified)
        {
            return OperationResult.Success("If an account needs verification, a new verification email has been sent.");
        }

        var token = await IssueVerificationTokenAsync(user, cancellationToken);
        await emailService.SendVerificationEmailAsync(user, token, cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.email.verification.resent",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: currentUserId,
            metadata: new { user.Email },
            cancellationToken);

        return OperationResult.Success("If an account needs verification, a new verification email has been sent.");
    }

    public async Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var genericMessage = "If an account exists for this email, a reset link has been sent.";
        var user = await dbContext.Users.SingleOrDefaultAsync(current => current.Email == email, cancellationToken);
        if (user is null)
        {
            await auditLogger.WriteAsync(
                action: "auth.password-reset.request.unknown-email",
                entityName: nameof(User),
                entityId: null,
                userId: null,
                metadata: new { email },
                cancellationToken);

            return OperationResult.Success(genericMessage);
        }

        var resetToken = await IssuePasswordResetTokenAsync(user, cancellationToken);
        await emailService.SendPasswordResetEmailAsync(user, resetToken, cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.password-reset.requested",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email },
            cancellationToken);

        return OperationResult.Success(genericMessage);
    }

    public async Task<OperationResult> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = passwordHashingService.HashToken(request.Token.Trim());
        var user = await dbContext.Users
            .Include(currentUser => currentUser.RefreshTokens)
            .SingleOrDefaultAsync(currentUser => currentUser.PasswordResetTokenHash == tokenHash, cancellationToken);

        if (user is null)
        {
            return OperationResult.Failure("This password reset link is invalid.", 400);
        }

        if (!user.PasswordResetTokenExpiresAtUtc.HasValue || user.PasswordResetTokenExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAtUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return OperationResult.Failure("This password reset link has expired.", 410);
        }

        user.PasswordHash = passwordHashingService.HashPassword(request.Password);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var revokedAtUtc = DateTime.UtcNow;
        foreach (var refreshToken in user.RefreshTokens.Where(refreshToken => refreshToken.RevokedAtUtc == null))
        {
            refreshToken.RevokedAtUtc = revokedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordChangedConfirmationAsync(user, cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.password-reset.completed",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email },
            cancellationToken);

        return OperationResult.Success("Your password has been reset successfully.");
    }

    public async Task<OperationResult> LogoutAsync(int userId, string? sessionId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        var activeTokensQuery = dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null);

        List<RefreshToken> tokensToRevoke;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            tokensToRevoke = await activeTokensQuery
                .Where(token => token.SessionId == sessionId)
                .ToListAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = passwordHashingService.HashToken(refreshToken);
            tokensToRevoke = await activeTokensQuery
                .Where(token => token.TokenHash == tokenHash)
                .ToListAsync(cancellationToken);
        }
        else
        {
            tokensToRevoke = await activeTokensQuery.ToListAsync(cancellationToken);
        }

        var revokedAtUtc = DateTime.UtcNow;
        foreach (var token in tokensToRevoke)
        {
            token.RevokedAtUtc = revokedAtUtc;
        }

        if (tokensToRevoke.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditLogger.WriteAsync(
            action: "auth.logout.succeeded",
            entityName: nameof(User),
            entityId: userId.ToString(),
            userId: userId,
            metadata: new
            {
                SessionId = sessionId ?? string.Empty,
                RevokedSessions = tokensToRevoke.Count
            },
            cancellationToken);

        return OperationResult.Success("Logout successful.");
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string message, CancellationToken cancellationToken)
    {
        var sessionId = jwtTokenFactory.CreateSessionId();
        var (token, expiresAtUtc) = jwtTokenFactory.CreateAccessToken(user, sessionId);
        var (refreshToken, refreshTokenExpiresAt) = jwtTokenFactory.CreateRefreshToken();

        await EnsureNotificationPreferencesAsync(user.Id, cancellationToken);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            SessionId = sessionId,
            TokenHash = passwordHashingService.HashToken(refreshToken),
            ExpiresAtUtc = refreshTokenExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAtUtc = expiresAtUtc,
            Role = user.Role.ToString(),
            Message = message,
            User = MapCurrentUser(user)
        };
    }

    private async Task<string> IssueVerificationTokenAsync(User user, CancellationToken cancellationToken)
    {
        var token = CreateSecureToken();
        user.EmailVerificationTokenHash = passwordHashingService.HashToken(token);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(authFlowSettings.EmailVerificationTokenLifetimeMinutes);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    private async Task<string> IssuePasswordResetTokenAsync(User user, CancellationToken cancellationToken)
    {
        var token = CreateSecureToken();
        user.PasswordResetTokenHash = passwordHashingService.HashToken(token);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(authFlowSettings.PasswordResetTokenLifetimeMinutes);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    private async Task EnsureNotificationPreferencesAsync(int userId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserNotificationPreferences.AnyAsync(preference => preference.UserId == userId, cancellationToken))
        {
            return;
        }

        dbContext.UserNotificationPreferences.Add(new UserNotificationPreference
        {
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private Task<User?> LoadUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(currentUser => currentUser.ManagedCenters.Where(centerManager => centerManager.Approved))
            .SingleOrDefaultAsync(currentUser => currentUser.Email == email, cancellationToken);
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
}
