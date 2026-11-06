using System.Runtime.Versioning;
using eNote.Application.Common.Files;
using eNote.Application.Common.Localization;
using eNote.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Storage;

public sealed class LocalFileStorageServiceRulesTests : IDisposable
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Webp = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50, 0, 0, 0, 0];
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "enote-rules-" + Guid.NewGuid().ToString("N"));

    public LocalFileStorageServiceRulesTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    public static TheoryData<byte[], string> MismatchedImages => new()
    {
        { Png, "image/jpeg" },
        { Jpeg, "image/png" },
        { Webp, "image/jpeg" },
        { Png, "image/webp" }
    };

    public static TheoryData<byte[], string> MismatchedAssignments => new()
    {
        { Pdf, "image/png" },
        { Png, "application/pdf" },
        { Jpeg, "application/pdf" }
    };

    public static TheoryData<byte[], string> InvalidContent => new()
    {
        { Zip, "image/png" },
        { Pdf, "image/png" },
        { new byte[] { 0x89 }, "image/png" },
        { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A }, "image/png" },
        { new byte[] { 0xFF, 0xD8 }, "image/jpeg" },
        { new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42 }, "image/webp" },
        { new byte[] { 0x00, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png" },
        { "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(), "image/png" },
        { "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(), "image/svg+xml" },
        { "GIF89a"u8.ToArray(), "image/gif" },
        { Array.Empty<byte>(), "image/png" }
    };

    [Theory]
    [MemberData(nameof(MismatchedImages))]
    public async Task SaveAsync_RefusesDeclaredTypeThatDiffersFromContent(byte[] content, string contentType)
    {
        using var stream = new MemoryStream(content);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().SaveAsync(stream, contentType, "instruments"));

        Assert.Equal(Messages.InvalidFileFormat, exception.Message);
        Assert.Empty(StoredFiles());
    }

    [Theory]
    [MemberData(nameof(MismatchedAssignments))]
    public async Task SaveAssignmentAsync_RefusesDeclaredTypeThatDiffersFromContent(byte[] content, string contentType)
    {
        using var stream = new MemoryStream(content);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().SaveAssignmentAsync(stream, contentType));

        Assert.Equal(Messages.AssignmentFileTypeNotAllowed, exception.Message);
        Assert.Empty(StoredFiles());
    }

    [Theory]
    [MemberData(nameof(InvalidContent))]
    public async Task SaveAsync_RefusesContentThatIsNotAnAllowedImage(byte[] content, string contentType)
    {
        using var stream = new MemoryStream(content);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().SaveAsync(stream, contentType, "instruments"));

        Assert.Equal(Messages.InvalidFileFormat, exception.Message);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task SaveAsync_AcceptsUpperCaseDeclaredType()
    {
        using var stream = new MemoryStream(Png);

        var path = await CreateService().SaveAsync(stream, "IMAGE/PNG", "instruments");

        Assert.EndsWith(".png", path);
    }

    [Fact]
    public async Task SaveAsync_AcceptsFileOfExactlyTheLimit()
    {
        var content = new byte[5_242_880];
        Png.CopyTo(content, 0);
        using var stream = new MemoryStream(content);

        var path = await CreateService().SaveAsync(stream, "image/png", "instruments");

        Assert.Equal(5_242_880, CreateService().GetFileSize(path));
    }

    [Fact]
    public async Task SaveAsync_RefusesFileOneByteOverTheLimit()
    {
        var content = new byte[5_242_881];
        Png.CopyTo(content, 0);
        using var stream = new MemoryStream(content);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().SaveAsync(stream, "image/png", "instruments"));

        Assert.Equal(Messages.FileTooLarge, exception.Message);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task SaveAsync_LeavesNoFile_WhenTheWriteFails()
    {
        using var stream = new FailingCopyStream(Png);

        await Assert.ThrowsAsync<IOException>(() => CreateService().SaveAsync(stream, "image/png", "instruments"));

        Assert.Empty(StoredFiles());
    }

    [Fact]
    [SupportedOSPlatform("linux")]
    public async Task Delete_DoesNotThrow_AndKeepsTheFile_WhenTheFolderIsReadOnly()
    {
        var service = CreateService();
        using var stream = new MemoryStream(Png);
        var path = await service.SaveAsync(stream, "image/png", "instruments");
        var folder = Path.Combine(_root, "uploads", "instruments");
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        try
        {
            service.Delete(path);
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Assert.Equal(Png.Length, service.GetFileSize(path));
    }

    [Fact]
    public void OpenRead_DoesNotReadASiblingFolderWithTheSamePrefix()
    {
        var sibling = Path.Combine(_root, "uploads-evil");
        Directory.CreateDirectory(sibling);
        File.WriteAllBytes(Path.Combine(sibling, "x.png"), Png);

        var (data, contentType) = CreateService().OpenRead($"{UploadPaths.RoutePrefix}/../uploads-evil/x.png");

        Assert.Null(data);
        Assert.Null(contentType);
    }

    [Fact]
    public void Limits_MatchTheStatedValues()
    {
        Assert.Equal(5_242_880, FileUploadLimits.MaxFileBytes);
        Assert.Equal(6_291_456, FileUploadLimits.MaxRequestBytes);
        Assert.Equal(524_288_000, FileUploadLimits.MaxStoreUploadBytes);
        Assert.Contains("500 MB", Messages.StorageQuotaExceeded);
        Assert.Contains("5 MB", Messages.FileTooLarge);
    }

    private LocalFileStorageService CreateService() =>
        new(Options.Create(new StorageOptions { Root = _root }), NullLogger<LocalFileStorageService>.Instance);

    private string[] StoredFiles() => Directory.GetFiles(_root, "*", SearchOption.AllDirectories);

    private sealed class FailingCopyStream(byte[] content) : MemoryStream(content)
    {
        private int _reads;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ++_reads == 1 ? base.ReadAsync(buffer, cancellationToken) : WriteSomeThenFail(buffer, cancellationToken);
        }

        private async ValueTask<int> WriteSomeThenFail(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_reads == 2)
            {
                return await base.ReadAsync(buffer[..4], cancellationToken);
            }

            throw new IOException("No space left on device.");
        }
    }
}
