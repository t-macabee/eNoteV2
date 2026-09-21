namespace eNote.Tests.TestUtils;

public sealed class StubCurrentActor(
    Student? student = null,
    Instructor? instructor = null,
    MusicStoreEmployee? employee = null,
    int? storeId = null,
    bool isAuthenticated = true,
    int? userId = null) : ICurrentUserContext, IStudentContext, IStoreContext
{
    public int UserId => userId ?? student?.AppUserId ?? 1;
    public bool IsAuthenticated => isAuthenticated;

    public Task<Student> GetCurrentStudentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(student ?? throw new NotSupportedException());

    public Task<int> GetCurrentStudentIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((student ?? throw new NotSupportedException()).Id);

    public Task<Instructor> GetCurrentInstructorAsync() =>
        Task.FromResult(instructor ?? throw new NotSupportedException());

    public Task<MusicStoreEmployee> GetCurrentEmployeeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(employee ?? throw new NotSupportedException());

    public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(storeId ?? 1);
}
