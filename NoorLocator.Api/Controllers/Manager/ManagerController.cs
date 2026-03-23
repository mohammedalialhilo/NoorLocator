using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;

namespace NoorLocator.Api.Controllers.Manager;

[ApiController]
[Route("api/manager")]
public class ManagerController(
    IManagerService managerService,
    IManagerCenterAccessService managerCenterAccessService) : ControllerBase
{
    [Authorize(Roles = "User,Admin")]
    [HttpPost("request")]
    public async Task<IActionResult> RequestAccess([FromBody] ManagerRequestDto request, CancellationToken cancellationToken)
    {
        var result = await managerService.RequestManagerAccessAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize(Policy = "ManagerArea")]
    [HttpGet("my-centers")]
    public async Task<IActionResult> GetMyCenters(CancellationToken cancellationToken)
    {
        var result = await managerCenterAccessService.GetManagedCentersAsync(User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
