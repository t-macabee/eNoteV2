using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/music-stores")]
public sealed class AdminMusicStoreController(MusicStoreService service, IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MusicStoreDto>>> GetPaged([FromQuery] MusicStoreSearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<MusicStoreDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MusicStoreDto>> GetById(int id, CancellationToken cancellationToken)
    {
        MusicStoreDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<MusicStoreDto>> Create([FromBody] MusicStoreRequest request, CancellationToken cancellationToken)
    {
        MusicStoreDto dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MusicStoreDto>> Update(int id, [FromBody] MusicStoreRequest request, CancellationToken cancellationToken)
    {
        MusicStoreDto dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/image")]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MusicStoreDto>> UploadImage(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await service.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateAdminMusicStoreReportAsync(cancellationToken);
        return File(pdf, "application/pdf", "music-stores-report.pdf");
    }
}
