namespace eNote.Application.Features.Identity.Users.Services;

public sealed class CurrentActor(ICurrentUserContext user, IUserProfileLookup lookup) : IStudentContext
{
    private Student? _student;
    private MusicStoreEmployee? _employee;

    public async Task<Student> GetCurrentStudentAsync() => _student ??= await lookup.GetStudentAsync(user.UserId);
    public async Task<int> GetCurrentStudentIdAsync() => (await GetCurrentStudentAsync()).Id;
    public async Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => _employee ??= await lookup.GetActiveEmployeeAsync(user.UserId);
}
