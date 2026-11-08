using eNote.API.Controllers.Base;
using eNote.API.Extensions;
using eNote.Application.Common.Files;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Application.Features.Rentals.ReferenceData.Manufacturers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Rentals;

public sealed class InstrumentController(
    InstrumentService instrumentService,
    RecommendationService recommendationService,
    InstrumentTypeService instrumentTypeService,
    ManufacturerService manufacturerService) : CoreController
{
    [HttpGet("~/api/v{version:apiVersion}/instruments")]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("~/api/v{version:apiVersion}/instruments/{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/instruments")]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetStorePaged([FromQuery] InstrumentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetPagedAsync(search, cancellationToken: cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/instruments/{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetStoreById(int id, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetByIdAsync(id, cancellationToken: cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/instruments")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] InstrumentRequest request, CancellationToken cancellationToken)
    {
        var result = await instrumentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStoreById), new { id = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPut("~/api/v{version:apiVersion}/shop/instruments/{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentRequest request, CancellationToken cancellationToken)
    {
        var result = await instrumentService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/instruments/{id:int}/image")]
    [RequestSizeLimit(FileUploadLimits.MaxRequestBytes)]
    [EnableRateLimiting(RateLimitingExtensions.UploadsPolicy)]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstrumentDto>> UploadImage(int id, IFormFile? file, CancellationToken ct) =>
        await UploadAsync(file, (stream, contentType, token) => instrumentService.UploadImageAsync(id, stream, contentType, token), ct);

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpDelete("~/api/v{version:apiVersion}/shop/instruments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await instrumentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/instrument-types")]
    [ProducesResponseType(typeof(PagedResult<InstrumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentTypeDto>>> GetInstrumentTypes(
        [FromQuery] InstrumentTypeSearchObject search, CancellationToken cancellationToken)
    {
        return Ok(await instrumentTypeService.GetPagedAsync(search, cancellationToken));
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/manufacturers")]
    [ProducesResponseType(typeof(PagedResult<ManufacturerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ManufacturerDto>>> GetManufacturers(
        [FromQuery] ManufacturerSearchObject search, CancellationToken cancellationToken)
    {
        return Ok(await manufacturerService.GetPagedAsync(search, cancellationToken));
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/instruments/recommended")]
    [ProducesResponseType(typeof(IReadOnlyList<InstrumentRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentRecommendationDto>>> GetRecommended([FromQuery] int count = RecommendationService.DefaultCount, CancellationToken cancellationToken = default)
    {
        var result = await recommendationService.GetRecommendedInstrumentsAsync(count, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/instruments/{id:int}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordView(int id, CancellationToken cancellationToken)
    {
        await recommendationService.RecordInstrumentViewAsync(id, cancellationToken);
        return NoContent();
    }
}
