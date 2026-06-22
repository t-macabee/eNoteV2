using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;

namespace eNote.API.Services
{
    public sealed class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
        private static readonly string[] AllowedAssignmentContentTypes = ["application/pdf", "image/jpeg", "image/png"];

        public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
        {
            if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
            {
                throw new BusinessException(Messages.FileTooLarge);
            }

            await ValidateImageMagicBytesAsync(stream);

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

            await ValidateAssignmentMagicBytesAsync(stream);

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

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "application/pdf" => ".pdf",
                    _ => ".bin"
                };
            }

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsRoot, uniqueName);

            stream.Position = 0;
            await using FileStream fileStream = File.Create(fullPath);
            await stream.CopyToAsync(fileStream, ct);

            return $"/api/uploads/{subfolder}/{uniqueName}";
        }

        private static async Task ValidateImageMagicBytesAsync(Stream stream)
        {
            var header = new byte[4];
            var read = await stream.ReadAsync(header.AsMemory(0, 4));

            if (read < 3)
            {
                throw new BusinessException(Messages.InvalidFileFormat);
            }

            var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            var isRiff = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

            if (!isJpeg && !isPng && !isRiff)
            {
                throw new BusinessException(Messages.InvalidFileFormat);
            }
        }

        private static async Task ValidateAssignmentMagicBytesAsync(Stream stream)
        {
            var header = new byte[4];
            var read = await stream.ReadAsync(header.AsMemory(0, 4));

            if (read < 3)
            {
                throw new BusinessException(Messages.InvalidFileFormat);
            }

            var isPdf = header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;
            var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            if (!isPdf && !isJpeg && !isPng)
            {
                throw new BusinessException(Messages.InvalidFileFormat);
            }
        }
    }
}
