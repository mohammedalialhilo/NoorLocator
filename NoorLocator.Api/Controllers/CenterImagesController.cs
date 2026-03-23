using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Api.Models.CenterImages;
using NoorLocator.Application.CenterImages.Dtos;
using NoorLocator.Application.CenterImages.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Handles manager-driven center gallery uploads and image management.
/// </summary>
[ApiController]
[Route("api/center-images")]
public class CenterImagesController(
    ICenterImageService centerImageService,
    IValidator<UploadCenterImageDto> uploadCenterImageValidator) : ControllerBase
{
    private const int MaxUploadBytes = 6 * 1024 * 1024;

    /// <summary>
    /// Uploads a center image for an assigned center.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<ApiResponse<CenterImageDto>>> Upload([FromForm] UploadCenterImageFormModel request, CancellationToken cancellationToken)
    {
        var payload = new UploadCenterImageDto
        {
            CenterId = request.CenterId,
            IsPrimary = request.IsPrimary
        };

        var validation = uploadCenterImageValidator.Validate(payload);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult<CenterImageDto>.Failure(validation.Errors.First(), 400));
        }

        var image = await request.Image.ToUploadPayloadAsync(cancellationToken);
        if (image is null)
        {
            return this.ToActionResult(OperationResult<CenterImageDto>.Failure("An image file is required.", 400));
        }

        var result = await centerImageService.UploadCenterImageAsync(payload, image, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Deletes a center image for an assigned center.
    /// Admins may also delete images for platform safety.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await centerImageService.DeleteCenterImageAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Sets a center image as the primary gallery image for its center.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPut("{id:int}/set-primary")]
    public async Task<ActionResult<ApiResponse<CenterImageDto>>> SetPrimary(int id, CancellationToken cancellationToken)
    {
        var result = await centerImageService.SetPrimaryImageAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
