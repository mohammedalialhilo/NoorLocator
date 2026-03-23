using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Content.Dtos;
using NoorLocator.Application.Content.Interfaces;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Exposes manifesto-backed site identity and About content for public pages.
/// </summary>
[ApiController]
[Route("api/content")]
public class ContentController(IAppContentService appContentService) : ControllerBase
{
    /// <summary>
    /// Returns structured About and homepage identity content.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("about")]
    public async Task<ActionResult<ApiResponse<AboutContentDto>>> GetAbout([FromQuery] string? languageCode, CancellationToken cancellationToken)
    {
        var result = await appContentService.GetAboutContentAsync(languageCode, cancellationToken);
        return this.ToActionResult(result);
    }
}
