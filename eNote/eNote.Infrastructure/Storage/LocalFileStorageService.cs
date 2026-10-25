using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Files;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using Microsoft.Extensions.Options;

namespace eNote.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly string[] AllowedImageContentTypes = [FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType, FileSignatureDetector.WebpMimeType];
    private static readonly string[] AllowedAssignmentContentTypes = [FileSignatureDetector.PdfMimeType, FileSignatureDetector.JpegMimeType, FileSignatureDetector.PngMimeType];

    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<StorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.Root);
    }

    public async Task<string> SaveAsync(Stream stream, string contentType, string subfolder, CancellationToken ct = default)
    {
        var detectedType = await ValidateAsync(stream, contentType, AllowedImageContentTypes, Messages.InvalidFileFormat, ct);

        return await SaveToDiskAsync(stream, subfolder, detectedType, ct);
    }

    public async Task<string> SaveAssignmentAsync(Stream stream, string contentType, CancellationToken ct = default)
    {
        var detectedType = await ValidateAsync(stream, contentType, AllowedAssignmentContentTypes, Messages.AssignmentFileTypeNotAllowed, ct);

        return await SaveToDiskAsync(stream, "assignments", detectedType, ct);
    }

    public (Stream? Data, string? ContentType) OpenRead(string path)
    {
        var fullPath = ResolveUploadPath(path);

        if (fullPath is null || !File.Exists(fullPath))
        {
            return (null, null);
        }

        var contentType = FileSignatureDetector.GetMimeType(Path.GetExtension(fullPath));

        return (File.OpenRead(fullPath), contentType);
    }

    public long GetFileSize(string path)
    {
        var fullPath = ResolveUploadPath(path);

        return fullPath is not null && File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
    }

    public void Delete(string path)
    {
        var fullPath = ResolveUploadPath(path);

        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private async Task<string> SaveToDiskAsync(Stream stream, string subfolder, string detectedType, CancellationToken ct)
    {
        var uploadsRoot = Path.Combine(_rootPath, "uploads", subfolder);

        Directory.CreateDirectory(uploadsRoot);

        var ext = FileSignatureDetector.GetExtension(detectedType) ?? throw new BusinessException(Messages.InvalidFileFormat);

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

        await using FileStream fileStream = File.Create(fullPath);

        await stream.CopyToAsync(fileStream, ct);
        return $"{UploadPaths.RoutePrefix}/{subfolder}/{uniqueName}";
    }

    private static async Task<string> ValidateAsync(Stream stream, string contentType, string[] allowedContentTypes, string invalidFormatMessage, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentType);

        if (!stream.CanSeek)
        {
            throw new ArgumentException("The upload stream must be seekable.", nameof(stream));
        }

        if (stream.Length > FileUploadLimits.MaxFileBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

        stream.Position = 0;

        var detectedType = FileSignatureDetector.DetectContentType(header.AsSpan(0, read));

        if (!allowedContentTypes.Contains(detectedType) || !allowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(invalidFormatMessage);
        }

        return detectedType;
    }

    private string? ResolveUploadPath(string path)
    {
        if (!path.StartsWith(UploadPaths.RoutePrefix + "/", StringComparison.Ordinal) || path.Contains('?', StringComparison.Ordinal) || path.Contains('#', StringComparison.Ordinal))
        {
            return null;
        }

        var relativePath = path[(UploadPaths.RoutePrefix + "/").Length..].Replace('/', Path.DirectorySeparatorChar);
        var uploadsRoot = Path.GetFullPath(Path.Combine(_rootPath, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));

        return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }
}
