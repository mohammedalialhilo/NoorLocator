using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new
        {
            application = "NoorLocator",
            status = "Healthy",
            environment = environment.EnvironmentName,
            timestampUtc = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(payload, "NoorLocator API is running."));
    }

    [AllowAnonymous]
    [HttpGet("ping")]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new { message = "pong" }, "Ping successful."));
    }
}
