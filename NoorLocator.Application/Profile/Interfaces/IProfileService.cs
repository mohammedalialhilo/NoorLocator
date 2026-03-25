using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Profile.Dtos;

namespace NoorLocator.Application.Profile.Interfaces;

public interface IProfileService
{
    Task<OperationResult<CurrentUserDto>> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<OperationResult<CurrentUserDto>> UpdateMyProfileAsync(int userId, UpdateProfileDto request, CancellationToken cancellationToken = default);
}
