using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;

namespace NoorLocator.Api.Controllers.Manager;

[ApiController]
[Route("api/manager")]
public class ManagerController(IManagerService managerService) : ControllerBase
{
    [Authorize(Roles = "User,Admin")]
    [HttpPost("request")]
    public async Task<IActionResult> RequestAccess([FromBody] ManagerRequestDto request, CancellationToken cancellationToken)
    {
        var result = await managerService.RequestManagerAccessAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpGet("ping")]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new { area = "manager" }, "Manager route wiring is active."));
    }
}
