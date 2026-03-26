using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Publishes lightweight health and readiness checks for the NoorLocator API host.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// Returns a verbose application health payload.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new Dictionary<string, object?>
        {
            ["application"] = "NoorLocator",
            ["status"] = "Healthy",
            ["timestampUtc"] = DateTime.UtcNow
        };

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            payload["environment"] = environment.EnvironmentName;
        }

        return Ok(ApiResponse<object>.SuccessResponse(payload, "NoorLocator API is running."));
    }

    /// <summary>
    /// Returns a lightweight pong response for connectivity checks.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("ping")]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new { message = "pong" }, "Ping successful."));
    }
}
