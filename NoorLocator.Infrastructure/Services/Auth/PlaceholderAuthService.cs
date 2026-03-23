using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Auth;

public class PlaceholderAuthService : IAuthService
{
    public Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult<AuthResponseDto>.NotImplemented(
                "Login is scaffolded for Phase 1. JWT issuance will be implemented in a later phase."));
    }

    public Task<OperationResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult<AuthResponseDto>.NotImplemented(
                "Registration is scaffolded for Phase 1. User creation will be implemented in a later phase."));
    }
}
