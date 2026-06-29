using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace eNote.Infrastructure.Storage;

public sealed class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageContentTypes = [FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType, FileSignatureDetector.WebpMimeType];
    private static readonly string[] AllowedAssignmentContentTypes = [FileSignatureDetector.PdfMimeType, FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType];

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
    {
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateMagicBytesAsync(stream, AllowedImageContentTypes, ct);

        if (!AllowedImageContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, fileName, contentType, subfolder, ct);
    }

    public async Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateMagicBytesAsync(stream, AllowedAssignmentContentTypes, ct);

        if (!AllowedAssignmentContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, fileName, contentType, "assignments", ct);
    }

    private async Task<string> SaveToDiskAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct)
    {
        var uploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", subfolder);
        Directory.CreateDirectory(uploadsRoot);

        var ext = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => throw new BusinessException(Messages.InvalidFileFormat)
        };

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

        stream.Position = 0;
        await using FileStream fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, ct);

        return $"/api/uploads/{subfolder}/{uniqueName}";
    }

    private static async Task ValidateMagicBytesAsync(Stream stream, string[] allowedContentTypes, CancellationToken ct)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        stream.Position = 0;

        if (!FileSignatureDetector.IsAllowed(header.AsSpan(0, read), allowedContentTypes))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }
    }
}