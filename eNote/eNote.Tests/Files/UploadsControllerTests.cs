using eNote.API.Controllers.Files;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Files.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Files;

public sealed class UploadsControllerTests
{
    [Fact]
    public void GetInstrument_ReturnsFileStream_WhenFileExists()
    {
        var files = new StubFileDownloadService();
        var controller = new UploadsController(files);

        var result = controller.GetInstrument("strat.webp");

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal([("instruments", "strat.webp")], files.Calls);
    }

    [Fact]
    public void GetMusicStore_ReturnsFileStream_WhenFileExists()
    {
        var files = new StubFileDownloadService();
        var controller = new UploadsController(files);

        var result = controller.GetMusicStore("store.webp");

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal([("music-stores", "store.webp")], files.Calls);
    }

    [Fact]
    public void GetAnnouncement_ReturnsFileStream_WhenFileExists()
    {
        var files = new StubFileDownloadService();
        var controller = new UploadsController(files);

        var result = controller.GetAnnouncement("news.webp");

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal([("announcements", "news.webp")], files.Calls);
    }

    [Fact]
    public async Task GetAssignment_ReturnsFileStream_WhenAccessGranted()
    {
        var files = new StubFileDownloadService { ContentType = "application/pdf" };
        var controller = new UploadsController(files);

        var result = await controller.GetAssignment("hw.pdf", CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(["hw.pdf"], files.AssignmentCalls);
    }

    [Fact]
    public void GetInstrument_PropagatesTheServiceFailure()
    {
        var files = new StubFileDownloadService { Error = new NotFoundException(Messages.NotFound) };
        var controller = new UploadsController(files);

        Assert.Throws<NotFoundException>(() => controller.GetInstrument("missing.webp"));
    }

    [Fact]
    public async Task GetAssignment_PropagatesTheServiceFailure()
    {
        var files = new StubFileDownloadService { Error = new AuthorizationException(Messages.Forbidden) };
        var controller = new UploadsController(files);

        await Assert.ThrowsAsync<AuthorizationException>(() => controller.GetAssignment("hw.pdf", CancellationToken.None));
    }

    private sealed class StubFileDownloadService : IFileDownloadService
    {
        public Stream Data { get; } = new MemoryStream([1, 2, 3]);
        public string ContentType { get; set; } = "image/webp";
        public Exception? Error { get; set; }
        public List<(string Subfolder, string FileName)> Calls { get; } = [];
        public List<string> AssignmentCalls { get; } = [];

        public (Stream Data, string ContentType) Get(string subfolder, string fileName)
        {
            Calls.Add((subfolder, fileName));

            return Error is null ? (Data, ContentType) : throw Error;
        }

        public Task<(Stream Data, string ContentType)> GetAssignmentAsync(
            string fileName,
            CancellationToken cancellationToken = default)
        {
            AssignmentCalls.Add(fileName);

            return Error is null
                ? Task.FromResult((Data, ContentType))
                : Task.FromException<(Stream Data, string ContentType)>(Error);
        }
    }
}
