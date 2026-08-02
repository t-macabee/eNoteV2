namespace eNote.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default);
    Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    (Stream? Data, string? ContentType) OpenRead(string path);
    void Delete(string path);
}
