using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/center-requests")]
public class CenterRequestsController(ICenterRequestService centerRequestService) : ControllerBase
{
    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCenterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await centerRequestService.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }
}
