using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Accepts user proposals for additional center languages.
/// </summary>
[ApiController]
[Route("api/center-language-suggestions")]
public class CenterLanguageSuggestionsController(
    IManagerService managerService,
    IValidator<CreateCenterLanguageSuggestionDto> suggestionValidator) : ControllerBase
{
    /// <summary>
    /// Creates a pending center-language suggestion for moderation.
    /// </summary>
    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] CreateCenterLanguageSuggestionDto request, CancellationToken cancellationToken)
    {
        var validation = suggestionValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await managerService.SuggestCenterLanguageAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
