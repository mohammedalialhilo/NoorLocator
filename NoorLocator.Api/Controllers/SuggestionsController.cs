using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Interfaces;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/suggestions")]
public class SuggestionsController(ISuggestionService suggestionService) : ControllerBase
{
    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSuggestionDto request, CancellationToken cancellationToken)
    {
        var result = await suggestionService.CreateAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
