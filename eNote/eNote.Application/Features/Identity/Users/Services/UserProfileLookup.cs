using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileLookup(IServiceProvider serviceProvider) : IUserProfileLookup
{
    private IAppDbContext Context => serviceProvider.GetRequiredService<IAppDbContext>();

    public async Task<Student> GetStudentAsync(int userId) =>
        await Context.Set<Student>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId) =>
        await Context.Set<Instructor>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new BusinessException(Messages.InstructorProfileNotFound);

    public async Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
        await Context.Set<MusicStoreEmployee>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
        ?? throw new BusinessException(Messages.EmployeeProfileNotFound);
}
