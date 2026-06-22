using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Files.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Files;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController(IWebHostEnvironment env, IFileAccessService fileAccess, ICurrentUserService currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("instruments/{fileName}")]
    public IActionResult GetInstrument(string fileName) => Serve("instruments", fileName);

    [Authorize]
    [HttpGet("announcements/{fileName}")]
    public IActionResult GetAnnouncement(string fileName) => Serve("announcements", fileName);

    [Authorize]
    [HttpGet("assignments/{fileName}")]
    public async Task<IActionResult> GetAssignment(string fileName, CancellationToken cancellationToken)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest();
        }

        if (!await fileAccess.CanAccessAssignmentFileAsync(currentUser.UserId, fileName, cancellationToken))
        {
            return Forbid();
        }

        return Serve("assignments", fileName);
    }

    private IActionResult Serve(string subfolder, string fileName)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest();
        }

        var uploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", subfolder);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, fileName));

        if (!fullPath.StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var contentType = GetContentType(fileName);
        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }

    private static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName)
        && fileName == Path.GetFileName(fileName)
        && !fileName.Contains("..", StringComparison.Ordinal);

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
}