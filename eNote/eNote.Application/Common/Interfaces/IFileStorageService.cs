namespace eNote.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default);
    }
}
