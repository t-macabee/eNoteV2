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
            .AsNoTracking();

        return await BuildPagedResultAsync(query, search, cancellationToken);
    }

    public async Task<PagedResult<StudentDto>> GetPagedForInstructorAsync(
        int instructorId,
        StudentSearchObject search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Student> query = StudentsVisibleTo(instructorId);

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

    public async Task<PagedResult<StudentEnrollmentDto>> GetEnrollmentsForInstructorAsync(
        int instructorId,
        int studentId,
        StudentEnrollmentSearchObject search,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentVisibleToInstructorAsync(instructorId, studentId, cancellationToken);

        var query = context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Course)
            .ThenInclude(c => c.Instructor)
            .Where(e => e.StudentId == studentId
                     && e.Course.InstructorId == instructorId
                     && e.EnrollmentStatus == EnrollmentStatus.Active);

        return await query.ToPagedResultAsync(
            search,
            async (enrollments, ct) =>
            {
                var users = await identityService.GetUsersBulkAsync(
                    enrollments.Select(e => e.Course.Instructor.AppUserId).Distinct(),
                    ct);

                return enrollments.Select(e => new StudentEnrollmentDto
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.Name,
                    InstructorId = e.Course.InstructorId,
                    InstructorName = UserNameHelper.FormatName(users.GetValueOrDefault(e.Course.Instructor.AppUserId))
                });
            },
            q => q.OrderBy(e => e.Course.Name),
            cancellationToken);
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

    internal IQueryable<Student> StudentsVisibleTo(int instructorId)
    {
        var instructorCourses = instructorAccess.CoursesFor(instructorId);

        return context.Set<Enrollment>()
            .AsNoTracking()
            .Where(e => e.EnrollmentStatus == EnrollmentStatus.Active || e.EnrollmentStatus == EnrollmentStatus.Completed)
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
        query = await query.WhereUserMatchesAsync(identityService, search.Name, search.IsActive, cancellationToken);

        return await query.ToPagedResultAsync(
            search,
            async (students, ct) =>
            {
                var users = await identityService.GetUsersBulkAsync(students.Select(x => x.AppUserId), ct);
                return students.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)));
            },
            ct: cancellationToken);
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
