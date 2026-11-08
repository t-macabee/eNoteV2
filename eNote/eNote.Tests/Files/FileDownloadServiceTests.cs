using eNote.Application.Common.Localization;
using eNote.Application.Features.Files.Services;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Files;

public sealed class FileDownloadServiceTests
{
    [Theory]
    [InlineData("../x.pdf")]
    [InlineData("sub/dir.pdf")]
    [InlineData("")]
    public void Get_ThrowsBusinessException_ForUnsafeFileNames(string fileName)
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1]), "application/pdf")
        };
        var service = CreateService(storage);

        var exception = Assert.Throws<BusinessException>(() => service.Get("instruments", fileName));

        Assert.Equal(Messages.InvalidFileName, exception.Message);
        Assert.Empty(storage.OpenReadCalls);
    }

    [Fact]
    public void Get_ThrowsNotFoundException_WhenFileIsMissing()
    {
        var storage = new RecordingFileStorageService { OpenReadResult = (null, null) };
        var service = CreateService(storage);

        Assert.Throws<NotFoundException>(() => service.Get("instruments", "missing.webp"));
    }

    [Fact]
    public void Get_ReturnsStreamAndContentType()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1, 2, 3]), "image/webp")
        };
        var service = CreateService(storage);

        var (data, contentType) = service.Get("instruments", "strat.webp");

        Assert.Equal("image/webp", contentType);
        Assert.Equal([1, 2, 3], ReadAll(data));
        Assert.Equal(["/api/v1/uploads/instruments/strat.webp"], storage.OpenReadCalls);
    }

    [Fact]
    public async Task GetAssignmentAsync_ThrowsAuthorizationException_ForForeignAssignmentFile()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1]), "application/pdf")
        };
        var service = CreateService(storage, canAccessAssignment: false);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.GetAssignmentAsync("hw.pdf"));
        Assert.Empty(storage.OpenReadCalls);
    }

    [Fact]
    public async Task GetAssignmentAsync_ThrowsBusinessException_ForUnsafeFileName_BeforeCheckingAccess()
    {
        var storage = new RecordingFileStorageService();
        var access = new StubFileAccessService(canAccess: true);
        var service = new FileDownloadService(storage, access, new StubCurrentActor(userId: 1));

        await Assert.ThrowsAsync<BusinessException>(() => service.GetAssignmentAsync("../hw.pdf"));
        Assert.Equal(0, access.CallCount);
    }

    [Fact]
    public async Task GetAssignmentAsync_ReturnsStream_ForOwnAssignmentFile()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1, 2, 3, 4]), "application/pdf")
        };
        var service = CreateService(storage, canAccessAssignment: true);

        var (data, contentType) = await service.GetAssignmentAsync("hw.pdf");

        Assert.Equal("application/pdf", contentType);
        Assert.Equal([1, 2, 3, 4], ReadAll(data));
        Assert.Equal(["/api/v1/uploads/assignments/hw.pdf"], storage.OpenReadCalls);
    }

    private static FileDownloadService CreateService(
        RecordingFileStorageService storage,
        bool canAccessAssignment = true) =>
        new(storage, new StubFileAccessService(canAccessAssignment), new StubCurrentActor(userId: 1));

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
