using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Profile.Dtos;
using NoorLocator.Application.Profile.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Allows authenticated users to view and update their own NoorLocator profile details.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(
    IProfileService profileService,
    IValidator<UpdateProfileDto> updateProfileValidator) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated user's editable profile details.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await profileService.GetMyProfileAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Updates the authenticated user's own profile fields.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> UpdateMyProfile([FromBody] UpdateProfileDto request, CancellationToken cancellationToken)
    {
        var validation = updateProfileValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult<CurrentUserDto>.Failure(validation.Errors.First(), 400));
        }

        var result = await profileService.UpdateMyProfileAsync(User.GetRequiredUserId(), request, cancellationToken);
        return this.ToActionResult(result);
    }
}
