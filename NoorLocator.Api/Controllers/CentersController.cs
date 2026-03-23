using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/centers")]
public class CentersController(ICenterService centerService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await centerService.GetCentersAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("nearest")]
    public async Task<IActionResult> GetNearest([FromQuery] decimal lat, [FromQuery] decimal lng, CancellationToken cancellationToken)
    {
        var result = await centerService.GetNearestCentersAsync(lat, lng, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await centerService.GetCenterByIdAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
