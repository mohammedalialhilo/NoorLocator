using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<VerifyEmailResultDto>> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    Task<OperationResult> ResendVerificationEmailAsync(int? currentUserId, string? email, CancellationToken cancellationToken = default);

    Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<OperationResult> LogoutAsync(int userId, string? sessionId, string? refreshToken, CancellationToken cancellationToken = default);
}
