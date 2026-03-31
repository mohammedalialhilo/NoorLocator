using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Notifications.Dtos;
using NoorLocator.Application.Notifications.Interfaces;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "VerifiedAccount")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationDto>>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await notificationService.GetMyNotificationsAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadNotificationCountDto>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await notificationService.GetUnreadCountAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<object?>>> MarkRead(int id, CancellationToken cancellationToken)
    {
        var result = await notificationService.MarkAsReadAsync(User.GetRequiredUserId(), id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object?>>> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await notificationService.MarkAllAsReadAsync(User.GetRequiredUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
