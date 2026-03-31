using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "VerifiedAccount")]
public class UsersController(IUserCenterEngagementService userCenterEngagementService) : ControllerBase
{
    [HttpGet("me/subscriptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CenterSubscriptionDto>>>> GetMySubscriptions(CancellationToken cancellationToken)
    {
        var result = await userCenterEngagementService.GetSubscriptionsAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
