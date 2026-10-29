namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileLookup(IAppDbContext context) : IUserProfileLookup
{
    public async Task<Student> GetStudentAsync(int userId, CancellationToken cancellationToken = default) =>
        await context.Set<Student>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken)
        ?? throw new BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId, CancellationToken cancellationToken = default) =>
        await context.Set<Instructor>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken)
        ?? throw new BusinessException(Messages.InstructorProfileNotFound);

    public Task<MusicStoreEmployee> GetEmployeeAsync(int userId, CancellationToken cancellationToken = default) => GetEmployeeAsync(context, userId, cancellationToken);

    public static async Task<MusicStoreEmployee> GetEmployeeAsync(
        IAppDbContext context,
        int userId,
        CancellationToken cancellationToken = default) =>
        await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken)
        ?? throw new BusinessException(Messages.EmployeeProfileNotFound);

    public static async Task<MusicStoreEmployee> EnsureManagerAsync(
        IAppDbContext context,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeAsync(context, userId, cancellationToken);

        if (!employee.IsManager)
        {
            throw new AuthorizationException(Messages.ManagerRoleRequired);
        }

        return employee;
    }
}
