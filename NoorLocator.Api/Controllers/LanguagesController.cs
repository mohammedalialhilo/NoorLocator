using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Languages.Interfaces;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Returns the predefined language catalog used across NoorLocator.
/// </summary>
[ApiController]
[Route("api/languages")]
public class LanguagesController(ILanguageService languageService) : ControllerBase
{
    /// <summary>
    /// Lists all predefined languages.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LanguageDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await languageService.GetAllAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
