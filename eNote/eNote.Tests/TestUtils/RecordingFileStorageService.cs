namespace eNote.Tests.TestUtils;

public sealed class RecordingFileStorageService : IFileStorageService
{
    public List<string> DeletedPaths { get; } = [];
    public List<(string FileName, string ContentType, string Subfolder)> SavedFiles { get; } = [];
    public List<string> SavedPaths { get; } = [];
    public List<string> OpenReadCalls { get; } = [];
    public Dictionary<string, long> FileSizes { get; } = [];
    public (Stream? Data, string? ContentType) OpenReadResult { get; set; } = (null, null);

    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
    {
        SavedFiles.Add((fileName, contentType, subfolder));
        var path = $"/api/v1/uploads/{subfolder}/{Guid.NewGuid()}";
        SavedPaths.Add(path);
        return Task.FromResult(path);
    }

    public Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        SavedFiles.Add((fileName, contentType, "assignments"));
        return Task.FromResult($"/api/v1/uploads/assignments/{Guid.NewGuid()}");
    }

    public (Stream? Data, string? ContentType) OpenRead(string path)
    {
        OpenReadCalls.Add(path);
        return OpenReadResult;
    }

    public long GetFileSize(string path) => FileSizes.GetValueOrDefault(path);

    public void Delete(string path) => DeletedPaths.Add(path);
}
