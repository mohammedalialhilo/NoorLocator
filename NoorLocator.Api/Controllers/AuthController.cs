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
    IValidator<LoginRequestDto> loginValidator,
    IValidator<ResendVerificationEmailRequestDto> resendVerificationValidator,
    IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
    IValidator<ResetPasswordRequestDto> resetPasswordValidator) : ControllerBase
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
    /// Verifies a user's email ownership by consuming a single-use verification token.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<ActionResult<ApiResponse<VerifyEmailResultDto>>> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyEmailAsync(token, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Sends a fresh verification email when an account is still awaiting verification.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("resend-verification-email")]
    public async Task<ActionResult<ApiResponse<object?>>> ResendVerificationEmail([FromBody] ResendVerificationEmailRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(resendVerificationValidator, request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        int? currentUserId = User.Identity?.IsAuthenticated == true
            ? User.GetRequiredUserId()
            : null;
        var result = await authService.ResendVerificationEmailAsync(currentUserId, request.Email, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Starts the password reset flow without revealing whether the email exists.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object?>>> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(forgotPasswordValidator, request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await authService.ForgotPasswordAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Resets a password by consuming a secure, expiring, single-use token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(resetPasswordValidator, request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await authService.ResetPasswordAsync(request, cancellationToken);
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
