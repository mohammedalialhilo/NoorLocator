using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Interfaces;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/majalis")]
public class MajalisController(IMajlisService majlisService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? centerId, CancellationToken cancellationToken)
    {
        var result = await majlisService.GetMajalisAsync(centerId, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMajlisDto request, CancellationToken cancellationToken)
    {
        var result = await majlisService.CreateMajlisAsync(request, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
