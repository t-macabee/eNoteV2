using eNote.API.Controllers.Files;
using eNote.Application.Features.Files.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Files;

// UploadsController previously read straight off disk via IWebHostEnvironment.WebRootPath,
// bypassing IFileStorageService, and had zero test coverage. These lock in the behavior of
// the refactored Serve() (now routed through IFileStorageService.OpenRead) so a future
// change can't silently regress the exact upload-path contract or drop range support.

public sealed class UploadsControllerTests
{
    [Fact]
    public void GetInstrument_ReturnsFileStream_WhenFileExists()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1, 2, 3]), "image/webp")
        };
        var controller = CreateController(storage);

        var result = controller.GetInstrument("strat.webp");

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(["/api/uploads/instruments/strat.webp"], storage.OpenReadCalls);
    }

    [Fact]
    public void GetInstrument_ReturnsNotFound_WhenFileMissing()
    {
        var storage = new RecordingFileStorageService { OpenReadResult = (null, null) };
        var controller = CreateController(storage);

        var result = controller.GetInstrument("missing.webp");

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData("../secrets.txt")]
    [InlineData("sub/dir.webp")]
    [InlineData("")]
    public void GetInstrument_ReturnsBadRequest_ForUnsafeFileNames(string fileName)
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1]), "image/webp")
        };
        var controller = CreateController(storage);

        var result = controller.GetInstrument(fileName);

        Assert.IsType<BadRequestResult>(result);
        Assert.Empty(storage.OpenReadCalls);
    }

    [Fact]
    public void GetAnnouncement_ReturnsFileStream_WhenFileExists()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1, 2, 3]), "image/png")
        };
        var controller = CreateController(storage);

        var result = controller.GetAnnouncement("banner.png");

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(["/api/uploads/announcements/banner.png"], storage.OpenReadCalls);
    }

    [Fact]
    public async Task GetAssignment_ReturnsForbid_WhenAccessDenied()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1]), "application/pdf")
        };
        var controller = CreateController(storage, canAccessAssignment: false);

        var result = await controller.GetAssignment("hw.pdf", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(storage.OpenReadCalls);
    }

    [Fact]
    public async Task GetAssignment_ReturnsBadRequest_ForUnsafeFileName_BeforeCheckingAccess()
    {
        var storage = new RecordingFileStorageService();
        var accessService = new StubFileAccessService(canAccess: true);
        var controller = CreateController(storage, accessService);

        var result = await controller.GetAssignment("../hw.pdf", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(0, accessService.CallCount);
    }

    [Fact]
    public async Task GetAssignment_ReturnsFileStream_WhenAccessGranted()
    {
        var storage = new RecordingFileStorageService
        {
            OpenReadResult = (new MemoryStream([1, 2, 3, 4]), "application/pdf")
        };
        var controller = CreateController(storage, canAccessAssignment: true);

        var result = await controller.GetAssignment("hw.pdf", CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(["/api/uploads/assignments/hw.pdf"], storage.OpenReadCalls);
    }

    [Fact]
    public async Task GetAssignment_ReturnsNotFound_WhenAccessGrantedButFileMissing()
    {
        var storage = new RecordingFileStorageService { OpenReadResult = (null, null) };
        var controller = CreateController(storage, canAccessAssignment: true);

        var result = await controller.GetAssignment("hw.pdf", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static UploadsController CreateController(
        RecordingFileStorageService storage,
        bool canAccessAssignment = true) =>
        CreateController(storage, new StubFileAccessService(canAccessAssignment));

    private static UploadsController CreateController(
        RecordingFileStorageService storage,
        IFileAccessService accessService) =>
        new(storage, accessService, new TestCurrentUserService(userId: 1));

    private sealed class TestCurrentUserService(int userId) : ICurrentUserService
    {
        public int UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class StubFileAccessService(bool canAccess) : IFileAccessService
    {
        public int CallCount { get; private set; }

        public Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(canAccess);
        }
    }
}
