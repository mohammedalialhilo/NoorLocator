using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers.Admin;

/// <summary>
/// Provides admin-only moderation, governance, and audit operations.
/// </summary>
[ApiController]
[Authorize(Policy = "AdminArea")]
[Route("api/admin")]
public class AdminController(
    IAdminService adminService,
    IValidator<UpdateCenterDto> updateCenterValidator) : ControllerBase
{
    /// <summary>
    /// Returns aggregate moderation, content, and audit metrics for the admin dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await adminService.GetDashboardAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists center requests for moderation review.
    /// </summary>
    [HttpGet("center-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminCenterRequestDto>>>> GetCenterRequests(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCenterRequestsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Approves a center request and publishes it as a live center.
    /// </summary>
    [HttpPost("center-requests/{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<object?>>> ApproveCenterRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveCenterRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Rejects a center request without publishing it.
    /// </summary>
    [HttpPost("center-requests/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<object?>>> RejectCenterRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectCenterRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists manager access requests awaiting review.
    /// </summary>
    [HttpGet("manager-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminManagerRequestDto>>>> GetManagerRequests(CancellationToken cancellationToken)
    {
        var result = await adminService.GetManagerRequestsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Approves a manager request and creates or updates the center assignment.
    /// </summary>
    [HttpPost("manager-requests/{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<object?>>> ApproveManagerRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveManagerRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Rejects a manager access request.
    /// </summary>
    [HttpPost("manager-requests/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<object?>>> RejectManagerRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectManagerRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists pending and historic center-language suggestions.
    /// </summary>
    [HttpGet("center-language-suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminCenterLanguageSuggestionDto>>>> GetCenterLanguageSuggestions(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCenterLanguageSuggestionsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Approves a center-language suggestion and adds it to the published center profile.
    /// </summary>
    [HttpPost("center-language-suggestions/{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<object?>>> ApproveCenterLanguageSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveCenterLanguageSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Rejects a center-language suggestion.
    /// </summary>
    [HttpPost("center-language-suggestions/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<object?>>> RejectCenterLanguageSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectCenterLanguageSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists application suggestions and their review status.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminSuggestionDto>>>> GetSuggestions(CancellationToken cancellationToken)
    {
        var result = await adminService.GetSuggestionsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Marks a user suggestion as reviewed.
    /// </summary>
    [HttpPut("suggestions/{id:int}/review")]
    public async Task<ActionResult<ApiResponse<object?>>> ReviewSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ReviewSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists users, roles, and summary account information.
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminUserDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var result = await adminService.GetUsersAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists the published centers that admins can maintain.
    /// </summary>
    [HttpGet("centers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminCenterDto>>>> GetCenters(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCentersAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing published center.
    /// </summary>
    [HttpPut("centers/{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateCenter(int id, [FromBody] UpdateCenterDto request, CancellationToken cancellationToken)
    {
        var validation = updateCenterValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await adminService.UpdateCenterAsync(id, request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Deletes a published center and its related management data.
    /// </summary>
    [HttpDelete("centers/{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteCenter(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.DeleteCenterAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns audit log entries for moderation, management, and auth-critical actions.
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminAuditLogDto>>>> GetAuditLogs(CancellationToken cancellationToken)
    {
        var result = await adminService.GetAuditLogsAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
