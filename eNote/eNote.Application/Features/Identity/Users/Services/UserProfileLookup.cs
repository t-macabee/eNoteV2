using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application.Features.Identity.Users.Services;

/// <summary>
/// Resolves <see cref="IAppDbContext"/> lazily via <see cref="IServiceProvider"/> rather than taking it
/// as a direct constructor parameter. <see cref="CurrentActor"/> constructor-injects this service, and
/// <c>ENoteContext</c> constructor-injects <see cref="ICurrentActor"/> for its tenant query filters — a
/// direct <c>IAppDbContext</c> dependency here would make DI eagerly build ENoteContext -> ICurrentActor
/// -> IUserProfileLookup -> IAppDbContext -> ENoteContext, a cycle hidden from DI's static cycle detector
/// behind the IAppDbContext factory registration, which hangs instead of failing fast.
/// </summary>
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
