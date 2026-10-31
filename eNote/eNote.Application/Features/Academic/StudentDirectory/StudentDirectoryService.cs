using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Academic.StudentDirectory;

public sealed class StudentDirectoryService(IAppDbContext context, IUserIdentityService identityService)
{
    public async Task<PagedResult<StudentDirectoryEntryDto>> GetPagedAsync(
        StudentDirectorySearchObject search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Student> query = context.Set<Student>()
            .AsNoTracking();

        query = await query.WhereUserMatchesAsync(identityService, search.Name, isActive: true, cancellationToken);

        return await query.ToPagedResultAsync(
            search,
            async (students, ct) =>
            {
                var users = await identityService.GetUsersBulkAsync(students.Select(x => x.AppUserId), ct);
                return students.Select(x => MapEntry(x, users.GetValueOrDefault(x.AppUserId)));
            },
            q => q.OrderBy(x => x.Id),
            cancellationToken);
    }

    public async Task<StudentProfileDto> GetProfileAsync(int studentId, CancellationToken cancellationToken = default)
    {
        Student? student = await context.Set<Student>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken);

        UserIdentityDto? user = student is null
            ? null
            : await identityService.GetUserAsync(student.AppUserId, cancellationToken);

        if (student is null || user is null || !user.IsActive)
        {
            throw new NotFoundException(Messages.StudentProfileNotFound);
        }

        var enrollments = await context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Course)
            .ThenInclude(c => c.Instructor)
            .Where(e => e.StudentId == studentId
                && (e.EnrollmentStatus == EnrollmentStatus.Active
                    || e.EnrollmentStatus == EnrollmentStatus.Canceled
                    || e.EnrollmentStatus == EnrollmentStatus.Completed))
            .ToListAsync(cancellationToken);

        Dictionary<int, CourseGradeStats> gradeData = await GradedSubmissionQueries.CountedGrades(context)
            .Where(s => s.StudentId == studentId)
            .GroupBy(s => s.Assignment.Lecture.CourseId)
            .Select(g =>
                new CourseGradeStats(g.Key,
                    g.Average(x => (double?)x.Grade),
                    g.Count()))
            .ToDictionaryAsync(x => x.CourseId, cancellationToken);

        var users = await identityService.GetUsersBulkAsync(
            enrollments.Select(e => e.Course.Instructor.AppUserId).Distinct(),
            cancellationToken);

        List<StudentCourseGradeDto> courses = [.. enrollments
            .Select(e =>
            {
                var stats = gradeData.GetValueOrDefault(e.CourseId);

                return new StudentCourseGradeDto
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.Name,
                    InstructorName = UserNameHelper.FormatName(users.GetValueOrDefault(e.Course.Instructor.AppUserId)),
                    EnrollmentStatus = e.EnrollmentStatus,
                    AverageGrade = GradedSubmissionQueries.RoundAverage(stats?.Average),
                    GradedSubmissions = stats?.Count ?? 0
                };
            })
            .OrderBy(c => c.CourseName)];

        return new StudentProfileDto
        {
            Id = student.Id,
            AppUserId = student.AppUserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Courses = courses
        };
    }

    private static StudentDirectoryEntryDto MapEntry(Student entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username
    };

    private sealed record CourseGradeStats(int CourseId, double? Average, int Count);
}
