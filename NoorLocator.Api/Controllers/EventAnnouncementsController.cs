using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Api.Models.EventAnnouncements;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.EventAnnouncements.Dtos;
using NoorLocator.Application.EventAnnouncements.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Publishes public center announcements and manager-authored announcement workflows.
/// </summary>
[ApiController]
[Route("api/event-announcements")]
public class EventAnnouncementsController(
    IEventAnnouncementService eventAnnouncementService,
    IValidator<CreateEventAnnouncementDto> createAnnouncementValidator,
    IValidator<UpdateEventAnnouncementDto> updateAnnouncementValidator) : ControllerBase
{
    /// <summary>
    /// Returns event announcements for a center.
    /// Public callers see published announcements only, while authorized managers can also see drafts and archived items for their assigned centers.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventAnnouncementDto>>>> GetByCenter([FromQuery] int centerId, CancellationToken cancellationToken)
    {
        if (centerId <= 0)
        {
            return this.ToActionResult(OperationResult<IReadOnlyCollection<EventAnnouncementDto>>.Failure("CenterId must be greater than zero.", 400));
        }

        var result = await eventAnnouncementService.GetAnnouncementsAsync(centerId, User.TryGetUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns a single event announcement.
    /// Unpublished announcements are only visible to authorized managers for the owning center or admins.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<EventAnnouncementDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await eventAnnouncementService.GetAnnouncementByIdAsync(id, User.TryGetUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Creates a new center announcement directly from an assigned manager without admin approval.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<EventAnnouncementDto>>> Create([FromForm] CreateEventAnnouncementFormModel request, CancellationToken cancellationToken)
    {
        var payload = new CreateEventAnnouncementDto
        {
            Title = request.Title,
            Description = request.Description,
            CenterId = request.CenterId,
            Status = request.Status
        };

        var validation = createAnnouncementValidator.Validate(payload);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult<EventAnnouncementDto>.Failure(validation.Errors.First(), 400));
        }

        var image = await request.Image.ToUploadPayloadAsync(cancellationToken);
        var result = await eventAnnouncementService.CreateAnnouncementAsync(payload, image, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing announcement within the manager's assigned-center scope.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<EventAnnouncementDto>>> Update(int id, [FromForm] UpdateEventAnnouncementFormModel request, CancellationToken cancellationToken)
    {
        var payload = new UpdateEventAnnouncementDto
        {
            Title = request.Title,
            Description = request.Description,
            CenterId = request.CenterId,
            Status = request.Status,
            RemoveImage = request.RemoveImage
        };

        var validation = updateAnnouncementValidator.Validate(payload);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult<EventAnnouncementDto>.Failure(validation.Errors.First(), 400));
        }

        var image = await request.Image.ToUploadPayloadAsync(cancellationToken);
        var result = await eventAnnouncementService.UpdateAnnouncementAsync(id, payload, image, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Deletes an announcement for an assigned center.
    /// Admins may also delete announcements for platform safety.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await eventAnnouncementService.DeleteAnnouncementAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
