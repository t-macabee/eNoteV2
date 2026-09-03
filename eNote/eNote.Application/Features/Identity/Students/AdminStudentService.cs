using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Identity.Students;

public sealed class AdminStudentService(
    IAppDbContext context,
    IUserIdentityService identityService,
    InstructorAccessService? instructorAccess = null)
{
    public async Task<PagedResult<StudentDto>> GetPagedAsync(StudentSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<Student> query = context.Set<Student>()
            .AsNoTracking()
            .OrderBy(x => x.Id);

        return await BuildPagedResultAsync(query, search, cancellationToken);
    }

    public async Task<PagedResult<StudentDto>> GetPagedForInstructorAsync(
        int instructorId,
        StudentSearchObject search,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instructorAccess);

        var instructorCourses = instructorAccess.CoursesFor(instructorId);

        IQueryable<Student> query = context.Set<Enrollment>()
            .AsNoTracking()
            .Join(
                instructorCourses,
                e => e.CourseId,
                c => c.Id,
                (e, c) => e.StudentId)
            .Distinct()
            .Join(
                context.Set<Student>().AsNoTracking(),
                studentId => studentId,
                s => s.Id,
                (studentId, s) => s)
            .OrderBy(x => x.Id);

        return await BuildPagedResultAsync(query, search, cancellationToken);
    }

    private async Task<PagedResult<StudentDto>> BuildPagedResultAsync(
        IQueryable<Student> query,
        StudentSearchObject search,
        CancellationToken cancellationToken)
    {
        List<Student> students = await query.ToListAsync(cancellationToken);
        IReadOnlyDictionary<int, UserIdentityDto> users = await identityService.GetUsersBulkAsync(students.Select(x => x.AppUserId), cancellationToken);

        List<StudentDto> filtered = [.. students
            .Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))
            .Where(x => MatchesName(x, search.Name))];

        (var page, var pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        return new PagedResult<StudentDto>
        {
            Items = [.. filtered.Skip((page - 1) * pageSize).Take(pageSize)],
            Page = page,
            PageSize = pageSize,
            TotalCount = search.IncludeTotalCount ? filtered.Count : null
        };
    }

    public async Task<StudentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Student entity = await context.Set<Student>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.StudentProfileNotFound);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId, cancellationToken);

        return Map(entity, user);
    }

    private static StudentDto Map(Student entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username,
        EnrollmentDate = entity.EnrollmentDate,
        MembershipPaidUntil = entity.MembershipPaidUntil
    };

    private static bool MatchesName(StudentDto dto, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var fullName = $"{dto.FirstName} {dto.LastName}".Trim();

        return Contains(dto.FirstName, name)
            || Contains(dto.LastName, name)
            || Contains(dto.Username, name)
            || Contains(fullName, name);
    }

    private static bool Contains(string? value, string name) => value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}
