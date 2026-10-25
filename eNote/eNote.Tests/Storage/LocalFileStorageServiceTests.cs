using eNote.Application.Common.Files;
using eNote.Application.Common.Localization;
using eNote.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Storage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] WebpBytes = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x00];
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A, 0x0A];

    private static readonly (byte[] Data, string ContentType, string Extension)[] ImageTypes =
    [
        (JpegBytes, "image/jpeg", ".jpg"),
        (PngBytes, "image/png", ".png"),
        (WebpBytes, "image/webp", ".webp")
    ];

    private static readonly (byte[] Data, string ContentType, string Extension)[] AssignmentTypes =
    [
        (PdfBytes, "application/pdf", ".pdf"),
        (JpegBytes, "image/jpeg", ".jpg"),
        (PngBytes, "image/png", ".png")
    ];

    private readonly string _webRoot;

    public LocalFileStorageServiceTests()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "enote-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRoot);
    }

    public static TheoryData<byte[], string, string> ImageFiles => ToTheoryData(ImageTypes);

    public static TheoryData<byte[], string, string> AssignmentFiles => ToTheoryData(AssignmentTypes);

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(ImageFiles))]
    public async Task SaveAsync_SavesAllowedImage_WithMatchingExtension_AndReadsItBack(byte[] data, string contentType, string extension)
    {
        var service = CreateService();
        using var stream = new MemoryStream(data);

        var path = await service.SaveAsync(stream, contentType, "profile-pictures");

        Assert.StartsWith($"{UploadPaths.RoutePrefix}/profile-pictures/", path);
        Assert.EndsWith(extension, path);
        Assert.True(File.Exists(Path.Combine(_webRoot, "uploads", "profile-pictures", Path.GetFileName(path))));

        var (read, readContentType) = service.OpenRead(path);

        Assert.NotNull(read);
        Assert.Equal(contentType, readContentType);
        read.Dispose();
    }

    [Theory]
    [MemberData(nameof(AssignmentFiles))]
    public async Task SaveAssignmentAsync_SavesAllowedFile_WithMatchingExtension_AndReadsItBack(byte[] data, string contentType, string extension)
    {
        var service = CreateService();
        using var stream = new MemoryStream(data);

        var path = await service.SaveAssignmentAsync(stream, contentType);

        Assert.StartsWith($"{UploadPaths.RoutePrefix}/assignments/", path);
        Assert.EndsWith(extension, path);

        var (read, readContentType) = service.OpenRead(path);

        Assert.NotNull(read);
        Assert.Equal(contentType, readContentType);
        read.Dispose();
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedContentType()
    {
        var service = CreateService();
        using var stream = new MemoryStream(PngBytes);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAsync(stream, "application/pdf", "profile-pictures"));
    }

    [Fact]
    public void OpenRead_ReturnsNull_WhenFileMissing()
    {
        var service = CreateService();

        var (data, contentType) = service.OpenRead($"{UploadPaths.RoutePrefix}/profile-pictures/missing.png");

        Assert.Null(data);
        Assert.Null(contentType);
    }

    [Fact]
    public void OpenRead_BlocksPathTraversal()
    {
        var service = CreateService();

        var (data, contentType) = service.OpenRead($"{UploadPaths.RoutePrefix}/../../secret.png");

        Assert.Null(data);
        Assert.Null(contentType);
    }

    [Fact]
    public async Task Delete_RemovesStoredFile()
    {
        var service = CreateService();
        using var stream = new MemoryStream(PngBytes);
        var path = await service.SaveAsync(stream, "image/png", "profile-pictures");

        service.Delete(path);

        Assert.False(File.Exists(Path.Combine(_webRoot, "uploads", "profile-pictures", Path.GetFileName(path))));
    }

    [Fact]
    public async Task SaveAssignmentAsync_RejectsWebp_WithAssignmentMessage()
    {
        var service = CreateService();
        using var stream = new MemoryStream(WebpBytes);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAssignmentAsync(stream, "image/webp"));

        Assert.Equal(Messages.AssignmentFileTypeNotAllowed, exception.Message);
    }

    [Fact]
    public async Task GetFileSize_ReturnsStoredFileLength()
    {
        var service = CreateService();
        using var stream = new MemoryStream(PngBytes);
        var path = await service.SaveAsync(stream, "image/png", "profile-pictures");

        Assert.Equal(stream.Length, service.GetFileSize(path));
    }

    [Fact]
    public void GetFileSize_ReturnsZero_WhenFileMissingOrPathUnsafe()
    {
        var service = CreateService();

        Assert.Equal(0, service.GetFileSize($"{UploadPaths.RoutePrefix}/profile-pictures/missing.png"));
        Assert.Equal(0, service.GetFileSize($"{UploadPaths.RoutePrefix}/../../secret.png"));
    }

    [Fact]
    public async Task SaveAsync_RejectsFileLargerThanLimit()
    {
        var service = CreateService();
        using var stream = new MemoryStream(new byte[FileUploadLimits.MaxFileBytes + 1]);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAsync(stream, "image/png", "profile-pictures"));

        Assert.Equal(Messages.FileTooLarge, ex.Message);
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentException_WhenStreamIsNotSeekable()
    {
        var service = CreateService();
        using var stream = new NonSeekableStream(new MemoryStream(PngBytes));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAsync(stream, "image/png", "profile-pictures"));
    }

    private static TheoryData<byte[], string, string> ToTheoryData((byte[] Data, string ContentType, string Extension)[] types)
    {
        var data = new TheoryData<byte[], string, string>();

        foreach (var (bytes, contentType, extension) in types)
        {
            data.Add(bytes, contentType, extension);
        }

        return data;
    }

    private LocalFileStorageService CreateService()
    {
        return new LocalFileStorageService(Options.Create(new StorageOptions { Root = _webRoot }));
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
