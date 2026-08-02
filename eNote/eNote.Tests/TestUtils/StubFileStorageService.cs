namespace eNote.Tests.TestUtils;

public sealed class StubFileStorageService : IFileStorageService
{
    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default) => Task.FromResult($"/uploads/{subfolder}/{Guid.NewGuid()}");
    public Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default) => Task.FromResult($"/uploads/assignments/{Guid.NewGuid()}");
    public (Stream? Data, string? ContentType) OpenRead(string path) => (null, null);
    public void Delete(string path) { }
}
