namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileLookup(IAppDbContext context) : IUserProfileLookup
{
    private IAppDbContext Context => context;

    public async Task<Student> GetStudentAsync(int userId) =>
        await Context.Set<Student>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId) =>
        await Context.Set<Instructor>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new BusinessException(Messages.InstructorProfileNotFound);

    public Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) => GetActiveEmployeeAsync(Context, userId);

    public static async Task<MusicStoreEmployee> GetActiveEmployeeAsync(
        IAppDbContext context,
        int userId,
        CancellationToken cancellationToken = default) =>
        await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive, cancellationToken)
        ?? throw new BusinessException(Messages.EmployeeProfileNotFound);

    public static async Task<MusicStoreEmployee> EnsureManagerAsync(
        IAppDbContext context,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetActiveEmployeeAsync(context, userId, cancellationToken);

        if (!employee.IsManager)
        {
            throw new AuthorizationException(Messages.ManagerRoleRequired);
        }

        return employee;
    }
}
