using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Handles authenticated center contribution requests.
/// </summary>
[ApiController]
[Route("api/center-requests")]
public class CenterRequestsController(
    ICenterRequestService centerRequestService,
    IValidator<CreateCenterRequestDto> centerRequestValidator) : ControllerBase
{
    /// <summary>
    /// Lists the current user's previously submitted center requests.
    /// </summary>
    [Authorize(Policy = "VerifiedAccount", Roles = "User,Manager,Admin")]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CenterRequestSummaryDto>>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await centerRequestService.GetMineAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Creates a pending center request for admin moderation.
    /// </summary>
    [Authorize(Policy = "VerifiedAccount", Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] CreateCenterRequestDto request, CancellationToken cancellationToken)
    {
        var validation = centerRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await centerRequestService.CreateAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
