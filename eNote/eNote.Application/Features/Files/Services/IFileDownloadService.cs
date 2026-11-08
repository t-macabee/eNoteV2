namespace eNote.Application.Features.Files.Services;

public interface IFileDownloadService
{
    (Stream Data, string ContentType) Get(string subfolder, string fileName);

    Task<(Stream Data, string ContentType)> GetAssignmentAsync(string fileName, CancellationToken cancellationToken = default);
}
