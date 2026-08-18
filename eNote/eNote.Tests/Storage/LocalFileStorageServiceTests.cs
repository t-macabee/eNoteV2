using eNote.Infrastructure.Storage;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Storage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _webRoot;

    public LocalFileStorageServiceTests()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "enote-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_SavesPng_UnderUploadsSubfolder()
    {
        var service = CreateService();
        using var stream = PngStream();

        var path = await service.SaveAsync(stream, "picture.png", "image/png", "profile-pictures");

        Assert.StartsWith("/api/uploads/profile-pictures/", path);
        Assert.True(File.Exists(Path.Combine(_webRoot, "uploads", "profile-pictures", Path.GetFileName(path))));
        Assert.EndsWith(".png", path);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedContentType()
    {
        var service = CreateService();
        using var stream = PngStream();

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAsync(stream, "file.pdf", "application/pdf", "profile-pictures"));
    }

    [Fact]
    public async Task OpenRead_ReturnsStreamAndContentType()
    {
        var service = CreateService();
        using var stream = PngStream();
        var path = await service.SaveAsync(stream, "picture.png", "image/png", "profile-pictures");

        var (data, contentType) = service.OpenRead(path);

        Assert.NotNull(data);
        Assert.Equal("image/png", contentType);
        data.Dispose();
    }

    [Fact]
    public void OpenRead_ReturnsNull_WhenFileMissing()
    {
        var service = CreateService();

        var (data, contentType) = service.OpenRead("/api/uploads/profile-pictures/missing.png");

        Assert.Null(data);
        Assert.Null(contentType);
    }

    [Fact]
    public void OpenRead_BlocksPathTraversal()
    {
        var service = CreateService();

        var (data, contentType) = service.OpenRead("/api/uploads/../../secret.png");

        Assert.Null(data);
        Assert.Null(contentType);
    }

    [Fact]
    public async Task Delete_RemovesStoredFile()
    {
        var service = CreateService();
        using var stream = PngStream();
        var path = await service.SaveAsync(stream, "picture.png", "image/png", "profile-pictures");

        service.Delete(path);

        Assert.False(File.Exists(Path.Combine(_webRoot, "uploads", "profile-pictures", Path.GetFileName(path))));
    }

    [Fact]
    public async Task SaveAssignmentAsync_SavesUnderAssignments()
    {
        var service = CreateService();
        using var stream = PdfStream();

        var path = await service.SaveAssignmentAsync(stream, "hw.pdf", "application/pdf");

        Assert.StartsWith("/api/uploads/assignments/", path);
        Assert.EndsWith(".pdf", path);
    }

    private LocalFileStorageService CreateService()
    {
        var env = new StubWebHostEnvironment
        {
            WebRootPath = _webRoot,
            ContentRootPath = _webRoot
        };
        return new LocalFileStorageService(env);
    }

    private static MemoryStream PngStream() =>
        new([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    private static MemoryStream PdfStream() =>
        new([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A, 0x0A]);
}
