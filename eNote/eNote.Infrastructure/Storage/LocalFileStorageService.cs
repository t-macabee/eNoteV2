using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageContentTypes = [FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType, FileSignatureDetector.WebpMimeType];
    private static readonly string[] AllowedAssignmentContentTypes = [FileSignatureDetector.PdfMimeType, FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType];

    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configuredRoot = configuration["Storage:Root"];

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = Path.Combine(Directory.GetCurrentDirectory(), "storage");
        }

        _rootPath = Path.GetFullPath(configuredRoot);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
    {
        if (!stream.CanSeek)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }
        if (stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateMagicBytesAsync(stream, AllowedImageContentTypes, ct);

        if (!AllowedImageContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, subfolder, ct);
    }

    public async Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!stream.CanSeek)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }
        if (stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateMagicBytesAsync(stream, AllowedAssignmentContentTypes, ct);

        if (!AllowedAssignmentContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, "assignments", ct);
    }

    public (Stream? Data, string? ContentType) OpenRead(string path)
    {
        var fullPath = ResolveUploadPath(path);

        if (fullPath is null || !File.Exists(fullPath))
        {
            return (null, null);
        }

        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => FileSignatureDetector.JpegMimeType,
            ".png" => FileSignatureDetector.PngMimeType,
            ".webp" => FileSignatureDetector.WebpMimeType,
            ".pdf" => FileSignatureDetector.PdfMimeType,
            _ => "application/octet-stream"
        };

        return (File.OpenRead(fullPath), contentType);
    }

    public void Delete(string path)
    {
        var fullPath = ResolveUploadPath(path);

        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private async Task<string> SaveToDiskAsync(Stream stream, string subfolder, CancellationToken ct)
    {
        var uploadsRoot = Path.Combine(_rootPath, "uploads", subfolder);

        Directory.CreateDirectory(uploadsRoot);

        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

        stream.Position = 0;

        var detectedType = FileSignatureDetector.DetectContentType(header.AsSpan(0, read));

        var ext = detectedType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => throw new BusinessException(Messages.InvalidFileFormat)
        };

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

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

    private string? ResolveUploadPath(string path)
    {
        if (!path.StartsWith("/api/uploads/", StringComparison.Ordinal) || path.Contains('?', StringComparison.Ordinal) || path.Contains('#', StringComparison.Ordinal))
        {
            return null;
        }

        var relativePath = path["/api/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var uploadsRoot = Path.GetFullPath(Path.Combine(_rootPath, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));

        return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }
}
