using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Application.Suggestions.Interfaces;

namespace NoorLocator.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(IManagerService managerService, ISuggestionService suggestionService) : ControllerBase
{
    [HttpPost("approve-manager")]
    public async Task<IActionResult> ApproveManager([FromBody] ApproveManagerRequestDto request, CancellationToken cancellationToken)
    {
        var result = await managerService.ApproveManagerAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("approve-language")]
    public async Task<IActionResult> ApproveLanguage([FromBody] ApproveLanguageSuggestionDto request, CancellationToken cancellationToken)
    {
        var result = await managerService.ApproveCenterLanguageSuggestionAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var result = await suggestionService.GetAllAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("ping")]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new { area = "admin" }, "Admin route wiring is active."));
    }
}
