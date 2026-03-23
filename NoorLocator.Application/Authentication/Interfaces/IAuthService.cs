using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
