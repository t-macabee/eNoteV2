namespace eNote.Application.Features.Identity.Users.Services;

public sealed class StudentDisplayNameService(IUserIdentityService identity) : IStudentDisplayNameService
{
    public async Task<string> GetStudentDisplayNameAsync(Student student)
    {
        var user = await identity.GetUserAsync(student.AppUserId);
        return user is null ? "Nepoznat korisnik" : FormatName(user);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students)
    {
        // One rental per row means the same student can appear several times;
        // ToDictionary below needs each id exactly once.
        List<Student> list = [.. students.DistinctBy(s => s.Id)];
        IReadOnlyDictionary<int, UserIdentityDto> users = await identity.GetUsersBulkAsync(list.Select(s => s.AppUserId));
        return list.ToDictionary(s => s.Id, s => users.TryGetValue(s.AppUserId, out UserIdentityDto? user) ? FormatName(user) : "Nepoznat korisnik");
    }

    private static string FormatName(UserIdentityDto user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }
}
