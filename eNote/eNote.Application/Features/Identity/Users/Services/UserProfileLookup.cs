using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileLookup(IAppDbContext context) : IUserProfileLookup
{
    public async Task<Student> GetStudentAsync(int userId) =>
        await context.Set<Student>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId) =>
        await context.Set<Instructor>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.InstructorProfileNotFound);

    public async Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
        await context.Set<MusicStoreEmployee>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
        ?? throw new Common.Exceptions.BusinessException(Messages.EmployeeProfileNotFound);
}
