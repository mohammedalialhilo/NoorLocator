using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.Infrastructure.Services.Auth;

public class AuthService(
    NoorLocatorDbContext dbContext,
    PasswordHashingService passwordHashingService,
    JwtTokenFactory jwtTokenFactory,
    AuditLogger auditLogger) : IAuthService
{
    public async Task<OperationResult<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(currentUser => currentUser.ManagedCenters.Where(centerManager => centerManager.Approved))
            .SingleOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);

        if (user is null)
        {
            return OperationResult<CurrentUserDto>.Failure("Authenticated user was not found.", 404);
        }

        return OperationResult<CurrentUserDto>.Success(MapCurrentUser(user));
    }

    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(currentUser => currentUser.ManagedCenters.Where(centerManager => centerManager.Approved))
            .SingleOrDefaultAsync(currentUser => currentUser.Email == email, cancellationToken);

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
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await BuildAuthResponseAsync(user, "Registration successful.", cancellationToken);

        await auditLogger.WriteAsync(
            action: "auth.register.succeeded",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            userId: user.Id,
            metadata: new { user.Email, Role = user.Role.ToString() },
            cancellationToken);

        return OperationResult<AuthResponseDto>.Success(response, "Registration successful.", 201);
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string message, CancellationToken cancellationToken)
    {
        var (token, expiresAtUtc) = jwtTokenFactory.CreateAccessToken(user);
        var (refreshToken, refreshTokenExpiresAt) = jwtTokenFactory.CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
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

    private static CurrentUserDto MapCurrentUser(User user)
    {
        return new CurrentUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            AssignedCenterIds = user.ManagedCenters
                .Where(centerManager => centerManager.Approved)
                .Select(centerManager => centerManager.CenterId)
                .Distinct()
                .ToArray()
        };
    }
}
