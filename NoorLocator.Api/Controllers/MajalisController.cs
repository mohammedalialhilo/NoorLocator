using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Api.Models.Majalis;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Exposes public majlis listings and manager-scoped majlis CRUD operations.
/// </summary>
[ApiController]
[Route("api/majalis")]
public class MajalisController(
    IMajlisService majlisService,
    IValidator<CreateMajlisDto> createMajlisValidator,
    IValidator<UpdateMajlisDto> updateMajlisValidator) : ControllerBase
{
    /// <summary>
    /// Returns public majalis, optionally filtered to a specific center.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MajlisDto>>>> Get([FromQuery] int? centerId, CancellationToken cancellationToken)
    {
        if (centerId is <= 0)
        {
            return this.ToActionResult(OperationResult<IReadOnlyCollection<MajlisDto>>.Failure("CenterId must be greater than zero.", 400));
        }

        var result = await majlisService.GetMajalisAsync(centerId, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns a single public majlis by identifier.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MajlisDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await majlisService.GetMajlisByIdAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Creates a new majlis for a manager's assigned center.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<object?>>> CreateJson([FromBody] CreateMajlisDto request, CancellationToken cancellationToken)
    {
        return await CreateInternalAsync(request, image: null, cancellationToken);
    }

    /// <summary>
    /// Creates a new majlis for a manager's assigned center with an optional image upload.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<object?>>> CreateForm([FromForm] CreateMajlisFormModel request, CancellationToken cancellationToken)
    {
        var payload = new CreateMajlisDto
        {
            Title = request.Title,
            Description = request.Description,
            Date = request.Date,
            Time = request.Time,
            CenterId = request.CenterId,
            LanguageIds = request.LanguageIds
        };

        var image = await request.Image.ToUploadPayloadAsync(cancellationToken);
        return await CreateInternalAsync(payload, image, cancellationToken);
    }

    /// <summary>
    /// Updates an existing majlis within the manager's authorized scope.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateJson(int id, [FromBody] UpdateMajlisDto request, CancellationToken cancellationToken)
    {
        return await UpdateInternalAsync(id, request, image: null, cancellationToken);
    }

    /// <summary>
    /// Updates an existing majlis within the manager's authorized scope with an optional image upload.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateForm(int id, [FromForm] UpdateMajlisFormModel request, CancellationToken cancellationToken)
    {
        var payload = new UpdateMajlisDto
        {
            Title = request.Title,
            Description = request.Description,
            Date = request.Date,
            Time = request.Time,
            CenterId = request.CenterId,
            LanguageIds = request.LanguageIds,
            RemoveImage = request.RemoveImage
        };

        var image = await request.Image.ToUploadPayloadAsync(cancellationToken);
        return await UpdateInternalAsync(id, payload, image, cancellationToken);
    }

    /// <summary>
    /// Deletes a majlis within the manager's authorized scope.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await majlisService.DeleteMajlisAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    private async Task<ActionResult<ApiResponse<object?>>> CreateInternalAsync(
        CreateMajlisDto request,
        UploadFilePayload? image,
        CancellationToken cancellationToken)
    {
        var validation = createMajlisValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await majlisService.CreateMajlisAsync(request, image, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    private async Task<ActionResult<ApiResponse<object?>>> UpdateInternalAsync(
        int id,
        UpdateMajlisDto request,
        UploadFilePayload? image,
        CancellationToken cancellationToken)
    {
        var validation = updateMajlisValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await majlisService.UpdateMajlisAsync(id, request, image, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }
}
