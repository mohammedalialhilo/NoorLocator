using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Handles account registration, sign-in, and current-user identity lookups.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequestDto> registerValidator,
    IValidator<LoginRequestDto> loginValidator) : ControllerBase
{
    /// <summary>
    /// Registers a new NoorLocator account with the default <c>User</c> role.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(registerValidator, request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Signs in an existing user and returns access and refresh tokens.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(loginValidator, request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns the currently authenticated user's profile and role scope.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Revokes the current authenticated session and clears the associated refresh token.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object?>>> Logout([FromBody] LogoutRequestDto? request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var sessionId = User.TryGetSessionId();
        var result = await authService.LogoutAsync(userId, sessionId, request?.RefreshToken, cancellationToken);
        return this.ToActionResult(result);
    }

    private ActionResult? Validate<T>(IValidator<T> validator, T instance)
    {
        var validation = validator.Validate(instance);
        if (validation.IsValid)
        {
            return null;
        }

        return BadRequest(ApiResponse<ApiErrorDetails>.Failure(
            validation.Errors.First(),
            new ApiErrorDetails
            {
                TraceId = HttpContext.TraceIdentifier,
                Errors = validation.Errors
            }));
    }
}
