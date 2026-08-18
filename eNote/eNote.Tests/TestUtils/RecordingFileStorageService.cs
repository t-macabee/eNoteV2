namespace eNote.Tests.TestUtils;

public sealed class RecordingFileStorageService : IFileStorageService
{
    public List<string> DeletedPaths { get; } = [];
    public List<(string FileName, string ContentType, string Subfolder)> SavedFiles { get; } = [];
    public List<string> OpenReadCalls { get; } = [];
    public (Stream? Data, string? ContentType) OpenReadResult { get; set; } = (null, null);

    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
    {
        SavedFiles.Add((fileName, contentType, subfolder));
        return Task.FromResult($"/api/uploads/{subfolder}/{Guid.NewGuid()}");
    }

    public Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        SavedFiles.Add((fileName, contentType, "assignments"));
        return Task.FromResult($"/api/uploads/assignments/{Guid.NewGuid()}");
    }

    public (Stream? Data, string? ContentType) OpenRead(string path)
    {
        OpenReadCalls.Add(path);
        return OpenReadResult;
    }

    public void Delete(string path) => DeletedPaths.Add(path);
}
