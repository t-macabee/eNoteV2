using eNote.Application.Features.Files.Services;

namespace eNote.Tests.TestUtils;

public sealed class StubFileAccessService(bool canAccess) : IFileAccessService
{
    public int CallCount { get; private set; }

    public Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(canAccess);
    }
}
