using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Interfaces;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/center-language-suggestions")]
public class CenterLanguageSuggestionsController(IManagerService managerService) : ControllerBase
{
    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCenterLanguageSuggestionDto request, CancellationToken cancellationToken)
    {
        var result = await managerService.SuggestCenterLanguageAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }
}
