using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Application.Features.Reports.Services;
using eNote.Domain.Entities.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/music-stores")]
public sealed class AdminMusicStoreController(MusicStoreService service, IReportService reportService)
    : ReferenceDataController<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(service)
{
    protected override int GetId(MusicStoreDto dto) => dto.Id;

    [HttpPost("{id:int}/image")]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MusicStoreDto>> UploadImage(int id, IFormFile? file, CancellationToken ct) =>
        await UploadAsync(file, (stream, fileName, contentType, token) => service.UploadImageAsync(id, stream, fileName, contentType, token), ct);

    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateAdminMusicStoreReportAsync(cancellationToken);
        return File(pdf, "application/pdf", "music-stores-report.pdf");
    }
}
