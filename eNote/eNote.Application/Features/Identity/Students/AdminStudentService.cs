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
        IQueryable<Student> query = StudentsVisibleTo(instructorId).OrderBy(x => x.Id);

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

        // 2. Return that student's enrollments for this instructor, filtered to EnrollmentStatus.Active.
        var enrollments = await context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Where(e => e.StudentId == studentId
                     && e.Course.InstructorId == instructorId
                     && e.EnrollmentStatus == EnrollmentStatus.Active)
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
                InstructorName = UserNameHelper.FormatName(users.GetValueOrDefault(e.Course.Instructor.AppUserId))
            })
            .OrderBy(e => e.CourseName)
            .ToList();
    }

    private async Task<Student> EnsureStudentVisibleToInstructorAsync(
        int instructorId,
        int studentId,
        CancellationToken cancellationToken)
    {
        return await StudentsVisibleTo(instructorId)
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);
    }

    private IQueryable<Student> StudentsVisibleTo(int instructorId)
    {
        var instructorCourses = instructorAccess.CoursesFor(instructorId);

        return context.Set<Enrollment>()
            .AsNoTracking()
            .Where(e => e.EnrollmentStatus != EnrollmentStatus.Canceled)
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
                (studentId, s) => s);
    }

    private async Task<PagedResult<StudentDto>> BuildPagedResultAsync(
        IQueryable<Student> query,
        StudentSearchObject search,
        CancellationToken cancellationToken)
    {
        List<Student> students = await query.ToListAsync(cancellationToken);
        IReadOnlyDictionary<int, UserIdentityDto> users = await identityService.GetUsersBulkAsync(students.Select(x => x.AppUserId), cancellationToken);

        List<StudentDto> mapped = [.. students.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))];

        return mapped.FilterAndPage(search, search.Name, search.IsActive);
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
}
