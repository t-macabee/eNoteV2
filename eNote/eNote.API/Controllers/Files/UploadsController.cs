using eNote.API.Controllers.Base;
using eNote.Application.Features.Files.Services;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Files;

[Route("api/v{version:apiVersion}/uploads")]
public sealed class UploadsController(IFileDownloadService fileDownload) : CoreController
{
    [HttpGet("instruments/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetInstrument(string fileName) => Serve("instruments", fileName);

    [HttpGet("music-stores/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetMusicStore(string fileName) => Serve("music-stores", fileName);

    [HttpGet("announcements/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAnnouncement(string fileName) => Serve("announcements", fileName);

    [HttpGet("courses/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCourse(string fileName) => Serve("courses", fileName);

    [HttpGet("events/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetEvent(string fileName) => Serve("events", fileName);

    [HttpGet("assignments/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignment(string fileName, CancellationToken cancellationToken)
    {
        var (data, contentType) = await fileDownload.GetAssignmentAsync(fileName, cancellationToken);

        return File(data, contentType, enableRangeProcessing: true);
    }

    private IActionResult Serve(string subfolder, string fileName)
    {
        var (data, contentType) = fileDownload.Get(subfolder, fileName);

        return File(data, contentType, enableRangeProcessing: true);
    }
}
