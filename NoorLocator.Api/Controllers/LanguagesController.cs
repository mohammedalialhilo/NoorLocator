using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Languages.Interfaces;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController(ILanguageService languageService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await languageService.GetAllAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
