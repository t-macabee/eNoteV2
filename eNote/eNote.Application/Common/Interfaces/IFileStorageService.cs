namespace eNote.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Stores an image and returns its uploads path. <paramref name="fileName"/> is accepted for
    /// transport symmetry and discarded: the stored name is server-generated.
    /// </summary>
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default);

    /// <summary>
    /// Stores a submission file and returns its uploads path. <paramref name="fileName"/> is accepted
    /// for transport symmetry and discarded: the stored name is server-generated.
    /// </summary>
    Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);

    (Stream? Data, string? ContentType) OpenRead(string path);

    long GetFileSize(string path);

    void Delete(string path);
}
