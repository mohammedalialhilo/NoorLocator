using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoorLocator.Api.Extensions;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Validation;

namespace NoorLocator.Api.Controllers;

[ApiController]
[Route("api/centers")]
public class CentersController(
    ICenterService centerService,
    IValidator<CenterLocationQueryDto> locationQueryValidator,
    IValidator<NearestCentersQueryDto> nearestQueryValidator,
    IValidator<CenterSearchQueryDto> searchQueryValidator) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CenterLocationQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(locationQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetCentersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("nearest")]
    public async Task<IActionResult> GetNearest([FromQuery] NearestCentersQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(nearestQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetNearestCentersAsync(query.Lat!.Value, query.Lng!.Value, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] CenterSearchQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(searchQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.SearchCentersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] CenterLocationQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = Validate(locationQueryValidator, query);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await centerService.GetCenterByIdAsync(id, query, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/majalis")]
    public async Task<IActionResult> GetMajalis(int id, CancellationToken cancellationToken)
    {
        var result = await centerService.GetCenterMajalisAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/languages")]
    public async Task<IActionResult> GetLanguages(int id, CancellationToken cancellationToken)
    {
        var result = await centerService.GetCenterLanguagesAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    private IActionResult? Validate<T>(IValidator<T> validator, T instance)
    {
        var validation = validator.Validate(instance);
        if (validation.IsValid)
        {
            return null;
        }

        return this.ToActionResult(OperationResult.Failure(validation.Errors.First(), 400));
    }
}
