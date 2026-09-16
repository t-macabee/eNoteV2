namespace eNote.Application.Features.Identity.Users.Services;

public sealed class StudentDisplayNameService(IUserIdentityService identity) : IStudentDisplayNameService
{
    public async Task<string> GetStudentDisplayNameAsync(Student student)
    {
        var user = await identity.GetUserAsync(student.AppUserId);
        return UserNameHelper.FormatName(user) ?? "Nepoznat korisnik";
    }

    public async Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students)
    {
        // One rental per row means the same student can appear several times;
        // ToDictionary below needs each id exactly once.
        List<Student> list = [.. students.DistinctBy(s => s.Id)];
        IReadOnlyDictionary<int, UserIdentityDto> users = await identity.GetUsersBulkAsync(list.Select(s => s.AppUserId));
        return list.ToDictionary(s => s.Id, s => UserNameHelper.FormatName(users.GetValueOrDefault(s.AppUserId)) ?? "Nepoznat korisnik");
    }
}
