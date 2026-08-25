using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Application.Features.Rentals.Recommendations.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Route("api/v{version:apiVersion}/instruments")]
public sealed class InstrumentController(
    InstrumentService instrumentService,
    RecommendationService recommendationService) : CoreController
{
    // ── Public actions (no auth required) ───────────────────────────

    [AllowAnonymous]
    [HttpGet("~/api/v{version:apiVersion}/instruments/public")]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPublicPaged([FromQuery] InstrumentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetPagedAsync(search, publicView: true, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("~/api/v{version:apiVersion}/instruments/public/{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetPublicById(int id, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetByIdAsync(id, publicView: true, cancellationToken);
        return Ok(result);
    }

    // ── Store employee actions ──────────────────────────────────────

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
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] InstrumentCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await instrumentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStoreById), new { id = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPut("~/api/v{version:apiVersion}/shop/instruments/{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await instrumentService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/instruments/{id:int}/image")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstrumentDto>> UploadImage(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await instrumentService.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpDelete("~/api/v{version:apiVersion}/shop/instruments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await instrumentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/instruments/recommended")]
    [ProducesResponseType(typeof(IReadOnlyList<InstrumentRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentRecommendationDto>>> GetRecommended([FromQuery] int count = 5, CancellationToken cancellationToken = default)
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
