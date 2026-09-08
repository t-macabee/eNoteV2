using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Identity.Students;

public sealed class AdminStudentService(
    IAppDbContext context,
    IUserIdentityService identityService,
    InstructorAccessService instructorAccess)
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

    public async Task<StudentDto> GetByIdForInstructorAsync(
        int instructorId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        Student entity = await EnsureStudentVisibleToInstructorAsync(instructorId, studentId, cancellationToken);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId, cancellationToken);

        return Map(entity, user);
    }

    public async Task<List<StudentEnrollmentDto>> GetEnrollmentsForInstructorAsync(
        int instructorId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        // 1. First, re-run the existing student-ownership gate: the student must be enrolled in one of this instructor's courses.
        // Deliberate asymmetry: the gate is on the student; the payload is not.
        await EnsureStudentVisibleToInstructorAsync(instructorId, studentId, cancellationToken);

        // 2. Return that student's enrollments across every instructor, filtered to EnrollmentStatus.Active.
        var enrollments = await context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .ToListAsync(cancellationToken);

        var appUserIds = enrollments
            .Select(e => e.Course.Instructor.AppUserId)
            .Distinct();

        var users = await identityService.GetUsersBulkAsync(appUserIds, cancellationToken);

        return enrollments
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseName = e.Course.Name,
                InstructorId = e.Course.InstructorId,
                InstructorName = FormatInstructorName(users.GetValueOrDefault(e.Course.Instructor.AppUserId))
            })
            .OrderBy(e => e.CourseName)
            .ToList();
    }

    private async Task<Student> EnsureStudentVisibleToInstructorAsync(
        int instructorId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var instructorCourses = instructorAccess.CoursesFor(instructorId);

        return await context.Set<Enrollment>()
            .AsNoTracking()
            .Join(
                instructorCourses,
                e => e.CourseId,
                c => c.Id,
                (e, c) => e.StudentId)
            .Distinct()
            .Join(
                context.Set<Student>().AsNoTracking(),
                sid => sid,
                s => s.Id,
                (sid, s) => s)
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);
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
            .Where(x => MatchesName(x, search.Name))
            .Where(x => !search.IsActive.HasValue || x.IsActive == search.IsActive.Value)];

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
        MembershipPaidUntil = entity.MembershipPaidUntil,
        IsActive = user?.IsActive ?? true
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

    private static string? FormatInstructorName(UserIdentityDto? user)
    {
        if (user is null)
        {
            return null;
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }
}
