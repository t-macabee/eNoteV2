using eNote.Application.Common.Files;

namespace eNote.Application.Features.Files.Services;

public sealed class FileDownloadService(
    IFileStorageService fileStorage,
    IFileAccessService fileAccess,
    ICurrentUserContext currentUser) : IFileDownloadService
{
    private const string AssignmentSubfolder = "assignments";

    public (Stream Data, string ContentType) Get(string subfolder, string fileName)
    {
        EnsureSafeFileName(fileName);

        return Open(subfolder, fileName);
    }

    public async Task<(Stream Data, string ContentType)> GetAssignmentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeFileName(fileName);

        if (!await fileAccess.CanAccessAssignmentFileAsync(currentUser.UserId, fileName, cancellationToken))
        {
            throw new AuthorizationException(Messages.Forbidden);
        }

        return Open(AssignmentSubfolder, fileName);
    }

    private (Stream Data, string ContentType) Open(string subfolder, string fileName)
    {
        var (data, contentType) = fileStorage.OpenRead($"{UploadPaths.RoutePrefix}/{subfolder}/{fileName}");

        if (data is null)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        return (data, contentType ?? "application/octet-stream");
    }

    private static void EnsureSafeFileName(string fileName)
    {
        if (!IsSafeFileName(fileName))
        {
            throw new BusinessException(Messages.InvalidFileName);
        }
    }

    private static bool IsSafeFileName(string fileName) => !string.IsNullOrWhiteSpace(fileName) && fileName == Path.GetFileName(fileName) && !fileName.Contains("..", StringComparison.Ordinal);
}
