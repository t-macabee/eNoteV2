using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class CurrentActor(ICurrentUserService user, IUserProfileLookup lookup, IAppDbContext context) : ICurrentActor
{
    private Student? _student;
    private Instructor? _instructor;
    private MusicStoreEmployee? _employee;
    private int? _storeId;

    public int UserId => user.UserId;
    public bool IsAuthenticated => user.IsAuthenticated;

    public async Task<Student> GetStudentAsync() => _student ??= await lookup.GetStudentAsync(user.UserId);
    public async Task<int> GetCurrentStudentIdAsync() => (await GetStudentAsync()).Id;
    public async Task<Instructor> GetInstructorAsync() => _instructor ??= await lookup.GetInstructorAsync(user.UserId);
    public async Task<MusicStoreEmployee> GetActiveEmployeeAsync() => _employee ??= await lookup.GetActiveEmployeeAsync(user.UserId);

    public async Task<int> GetActiveStoreAsync()
    {
        if (_storeId is not null) return _storeId.Value;

        var storeId = await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .Where(x => x.AppUserId == user.UserId && x.IsActive)
            .Select(x => x.MusicStoreId)
            .SingleOrDefaultAsync();

        if (storeId == 0) throw new BusinessException(Messages.ActiveEmployeeStoreNotFound);

        _storeId = storeId;
        return storeId;
    }
}
