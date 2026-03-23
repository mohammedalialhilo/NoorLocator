using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

/// <summary>
/// Exposes the public center discovery and center profile API surface.
/// </summary>
[ApiController]
[Route("api/centers")]
public class CentersController(
    ICenterService centerService,
    IValidator<CenterLocationQueryDto> locationQueryValidator,
    IValidator<NearestCentersQueryDto> nearestQueryValidator,
    IValidator<CenterSearchQueryDto> searchQueryValidator) : ControllerBase
{
    /// <summary>
    /// Returns the published center directory, optionally including distance from a provided location.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CenterSummaryDto>>>> GetAll([FromQuery] CenterLocationQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(locationQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetCentersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns the nearest published centers for a latitude and longitude pair.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("nearest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CenterSummaryDto>>>> GetNearest([FromQuery] NearestCentersQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(nearestQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetNearestCentersAsync(query.Lat!.Value, query.Lng!.Value, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Searches published centers by keyword, place, language, and optional location context.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CenterSummaryDto>>>> Search([FromQuery] CenterSearchQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(searchQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.SearchCentersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Returns a single center profile including public languages and upcoming majalis.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CenterDetailsDto>>> GetById(int id, [FromQuery] CenterLocationQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(locationQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetCenterByIdAsync(id, query, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists the upcoming public majalis published for a center.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}/majalis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MajlisDto>>>> GetMajalis(int id, CancellationToken cancellationToken)
    {
        var result = await centerService.GetCenterMajalisAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lists the published languages supported by a center.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}/languages")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LanguageDto>>>> GetLanguages(int id, CancellationToken cancellationToken)
    {
        var result = await centerService.GetCenterLanguagesAsync(id, cancellationToken);
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
