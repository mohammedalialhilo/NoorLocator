using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return this.ToActionResult(result);
    }
}
