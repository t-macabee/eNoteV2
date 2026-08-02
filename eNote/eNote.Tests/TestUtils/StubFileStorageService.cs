namespace eNote.Tests.TestUtils;

public sealed class StubFileStorageService : IFileStorageService
{
    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default) => Task.FromResult($"/uploads/{subfolder}/{Guid.NewGuid()}");
    public Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default) => Task.FromResult($"/uploads/assignments/{Guid.NewGuid()}");
    public Task<(Stream? Data, string? ContentType)> OpenReadAsync(string path, CancellationToken ct = default) => Task.FromResult<(Stream? Data, string? ContentType)>((null, null));
    public Task DeleteAsync(string path, CancellationToken ct = default) => Task.CompletedTask;
}
