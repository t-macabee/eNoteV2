using eNote.Application.Common.Interfaces;
using eNote.Domain.Entities;

namespace eNote.Tests.TestUtils;

public sealed class StubCurrentActor(
    Student? student = null,
    Instructor? instructor = null,
    MusicStoreEmployee? employee = null,
    int? storeId = null,
    bool isAuthenticated = true) : ICurrentActor
{
    public int UserId => student?.AppUserId ?? 1;
    public bool IsAuthenticated => isAuthenticated;

    public Task<Student> GetCurrentStudentAsync() =>
        Task.FromResult(student ?? throw new NotSupportedException());

    public Task<int> GetCurrentStudentIdAsync() =>
        Task.FromResult((student ?? throw new NotSupportedException()).Id);

    public Task<Instructor> GetCurrentInstructorAsync() =>
        Task.FromResult(instructor ?? throw new NotSupportedException());

    public Task<MusicStoreEmployee> GetCurrentEmployeeAsync() =>
        Task.FromResult(employee ?? throw new NotSupportedException());

    public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(storeId ?? 1);

    public int GetCurrentStoreId() => storeId ?? 1;
}
