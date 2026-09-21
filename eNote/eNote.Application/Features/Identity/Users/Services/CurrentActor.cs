namespace eNote.Application.Features.Identity.Users.Services;

public sealed class CurrentActor(ICurrentUserContext user, IUserProfileLookup lookup) : IStudentContext
{
    private Student? _student;
    private MusicStoreEmployee? _employee;

    public async Task<Student> GetCurrentStudentAsync(CancellationToken cancellationToken = default) => _student ??= await lookup.GetStudentAsync(user.UserId, cancellationToken);
    public async Task<int> GetCurrentStudentIdAsync(CancellationToken cancellationToken = default) => (await GetCurrentStudentAsync(cancellationToken)).Id;
    public async Task<MusicStoreEmployee> GetCurrentEmployeeAsync(CancellationToken cancellationToken = default) => _employee ??= await lookup.GetActiveEmployeeAsync(user.UserId, cancellationToken);
}
