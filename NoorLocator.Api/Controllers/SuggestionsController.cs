using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Accepts authenticated feedback about the application and directory data.
/// </summary>
[ApiController]
[Route("api/suggestions")]
public class SuggestionsController(
    ISuggestionService suggestionService,
    IValidator<CreateSuggestionDto> suggestionValidator) : ControllerBase
{
    /// <summary>
    /// Submits a new suggestion for admin review.
    /// </summary>
    [Authorize(Policy = "VerifiedAccount", Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] CreateSuggestionDto request, CancellationToken cancellationToken)
    {
        var validation = suggestionValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await suggestionService.CreateAsync(request, User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
