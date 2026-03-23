using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Centers.Dtos;

namespace NoorLocator.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await adminService.GetDashboardAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("center-requests")]
    public async Task<IActionResult> GetCenterRequests(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCenterRequestsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("center-requests/{id:int}/approve")]
    public async Task<IActionResult> ApproveCenterRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveCenterRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("center-requests/{id:int}/reject")]
    public async Task<IActionResult> RejectCenterRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectCenterRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("manager-requests")]
    public async Task<IActionResult> GetManagerRequests(CancellationToken cancellationToken)
    {
        var result = await adminService.GetManagerRequestsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("manager-requests/{id:int}/approve")]
    public async Task<IActionResult> ApproveManagerRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveManagerRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("manager-requests/{id:int}/reject")]
    public async Task<IActionResult> RejectManagerRequest(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectManagerRequestAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("center-language-suggestions")]
    public async Task<IActionResult> GetCenterLanguageSuggestions(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCenterLanguageSuggestionsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("center-language-suggestions/{id:int}/approve")]
    public async Task<IActionResult> ApproveCenterLanguageSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ApproveCenterLanguageSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("center-language-suggestions/{id:int}/reject")]
    public async Task<IActionResult> RejectCenterLanguageSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.RejectCenterLanguageSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var result = await adminService.GetSuggestionsAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("suggestions/{id:int}/review")]
    public async Task<IActionResult> ReviewSuggestion(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.ReviewSuggestionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await adminService.GetUsersAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("centers")]
    public async Task<IActionResult> GetCenters(CancellationToken cancellationToken)
    {
        var result = await adminService.GetCentersAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("centers/{id:int}")]
    public async Task<IActionResult> UpdateCenter(int id, [FromBody] UpdateCenterDto request, CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateCenterAsync(id, request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("centers/{id:int}")]
    public async Task<IActionResult> DeleteCenter(int id, CancellationToken cancellationToken)
    {
        var result = await adminService.DeleteCenterAsync(id, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(CancellationToken cancellationToken)
    {
        var result = await adminService.GetAuditLogsAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
