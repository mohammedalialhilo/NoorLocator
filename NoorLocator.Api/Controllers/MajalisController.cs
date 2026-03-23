using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
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
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] CreateMajlisDto request, CancellationToken cancellationToken)
    {
        var validation = createMajlisValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await majlisService.CreateMajlisAsync(request, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing majlis within the manager's authorized scope.
    /// </summary>
    [Authorize(Policy = "ManagerArea")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Update(int id, [FromBody] UpdateMajlisDto request, CancellationToken cancellationToken)
    {
        var validation = updateMajlisValidator.Validate(request);
        if (!validation.IsValid)
        {
            return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
        }

        var result = await majlisService.UpdateMajlisAsync(id, request, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return this.ToActionResult(result);
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
}
