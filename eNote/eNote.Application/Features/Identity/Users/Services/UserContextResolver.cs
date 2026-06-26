using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public class UserContextResolver(IAppDbContext context, IUserIdentityService identity) : IUserContextResolver
{
    public async Task<int> GetCurrentStudentIdAsync(int appUserId) =>
        (await GetStudentAsync(appUserId)).Id;

    public async Task<Student> GetStudentAsync(int userId) =>
        await context.Set<Student>()
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId) =>
        await context.Set<Instructor>()
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.InstructorProfileNotFound);

    public async Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
        await context.Set<MusicStoreEmployee>()
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
        ?? throw new Common.Exceptions.BusinessException(Messages.EmployeeProfileNotFound);

    public async Task<string> GetStudentDisplayNameAsync(Student student)
    {
        var user = await identity.GetUserAsync(student.AppUserId);

        return user is null ? $"Student {student.Id}" : FormatName(user);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students)
    {
        List<Student> list = [.. students];

        IReadOnlyDictionary<int, UserIdentityDto> users = await identity.GetUsersBulkAsync(list.Select(s => s.AppUserId));

        return list.ToDictionary(s => s.Id, s => users.TryGetValue(s.AppUserId, out UserIdentityDto? user) ? FormatName(user) : $"Student {s.Id}");
    }

    private static string FormatName(UserIdentityDto user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }
}
