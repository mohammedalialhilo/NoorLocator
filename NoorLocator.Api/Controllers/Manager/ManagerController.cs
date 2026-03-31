using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers.Manager;

/// <summary>
/// Exposes manager access requests and center-assignment views.
/// </summary>
[ApiController]
[Route("api/manager")]
public class ManagerController(
    IManagerService managerService,
    IManagerCenterAccessService managerCenterAccessService,
    IValidator<ManagerRequestDto> managerRequestValidator) : ControllerBase
{
    /// <summary>
    /// Requests manager access for a specific center.
    /// </summary>
    [Authorize(Policy = "VerifiedAccount", Roles = "User,Admin")]
    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<object?>>> RequestAccess([FromBody] ManagerRequestDto request, CancellationToken cancellationToken)
    {
        var validation = managerRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await managerService.RequestManagerAccessAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns the centers the current manager can maintain.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpGet("my-centers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ManagedCenterDto>>>> GetMyCenters(CancellationToken cancellationToken)
    {
        var result = await managerCenterAccessService.GetManagedCentersAsync(User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
