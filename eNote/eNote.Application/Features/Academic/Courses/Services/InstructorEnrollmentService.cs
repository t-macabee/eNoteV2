using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class InstructorEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser,
    InstructorAccessService instructorAccess,
    IStudentDisplayNameService displayNames,
    IEnrollmentNotificationDispatcher notificationDispatcher,
    IUserIdentityService identity)
{
    public async Task<PagedResult<CourseEnrollmentDto>> GetForCourseAsync(int courseId, CourseEnrollmentSearchObject search, CancellationToken ct = default)
    {
        await EnsureOwnsCourseAsync(courseId, ct);

        var query = context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.CourseId == courseId);

        if (search.EnrollmentStatus.HasValue)
        {
            query = query.Where(e => e.EnrollmentStatus == search.EnrollmentStatus.Value);
        }

        query = await ApplyStudentNameAsync(query, search.StudentName, ct);

        return await PageAsync(query, search, ct);
    }

    public async Task<PagedResult<CourseEnrollmentDto>> GetForInstructorAsync(InstructorEnrollmentSearchObject search, CancellationToken ct = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, ct);

        IQueryable<Enrollment> query = instructorAccess.EnrollmentsFor(instructorId)
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course);

        if (search.EnrollmentStatus.HasValue)
        {
            query = query.Where(e => e.EnrollmentStatus == search.EnrollmentStatus.Value);
        }

        if (search.CourseId.HasValue)
        {
            query = query.Where(e => e.CourseId == search.CourseId.Value);
        }

        query = await ApplyStudentNameAsync(query, search.StudentName, ct);

        return await PageAsync(query, search, ct);
    }

    private async Task<IQueryable<Enrollment>> ApplyStudentNameAsync(IQueryable<Enrollment> query, string? studentName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(studentName))
        {
            return query;
        }

        var userIds = await identity.FindUserIdsAsync(studentName, null, ct);

        return query.Where(e => userIds.Contains(e.Student!.AppUserId));
    }

    public Task<CourseEnrollmentDto> ApproveAsync(int courseId, int enrollmentId, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Approve, reason: null, ct);

    public Task<CourseEnrollmentDto> RejectAsync(int courseId, int enrollmentId, EnrollmentReasonRequest request, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Reject, request.Reason, ct);

    public Task<CourseEnrollmentDto> CancelAsync(int courseId, int enrollmentId, EnrollmentReasonRequest request, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Cancel, request.Reason, ct);

    public Task<CourseEnrollmentDto> CompleteAsync(int courseId, int enrollmentId, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Complete, reason: null, ct);

    private async Task<CourseEnrollmentDto> DecideAsync(int courseId, int enrollmentId, EnrollmentTrigger trigger, string? reason, CancellationToken ct)
    {
        await EnsureOwnsCourseAsync(courseId, ct);

        var enrollment = await context.Set<Enrollment>()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.CourseId == courseId, ct)
            ?? throw new NotFoundException(Messages.EnrollmentNotFound);

        if (trigger == EnrollmentTrigger.Approve && enrollment.Course.HasEnded(clock.UtcNow))
        {
            throw new BusinessException(Messages.CourseEnded);
        }

        var result = enrollment.Transition(trigger, EnrollmentActor.Instructor, currentUser.UserId, clock.UtcNow, reason);

        if (!result.IsSuccess)
        {
            throw new BusinessException(result.Error);
        }

        await notificationDispatcher.DispatchStatusChangedAsync(enrollment.Id, enrollment.CourseId, enrollment.Student.AppUserId, enrollment.Course.Name, result.Value, StatusReason(trigger, enrollment));

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }

        return Map(enrollment, await displayNames.GetStudentDisplayNameAsync(enrollment.Student, ct));
    }

    private async Task<PagedResult<CourseEnrollmentDto>> PageAsync(IQueryable<Enrollment> query, BaseSearchObject search, CancellationToken ct)
    {
        return await query.ToPagedResultAsync(
            search,
            async (enrollments, token) =>
            {
                var names = await displayNames.GetStudentDisplayNamesAsync(enrollments.Select(e => e.Student!), token);
                return enrollments.Select(e => Map(e, names.GetValueOrDefault(e.StudentId, Messages.UnknownUserName)));
            },
            ct: ct);
    }

    private async Task EnsureOwnsCourseAsync(int courseId, CancellationToken ct)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, ct);
        await instructorAccess.EnsureOwnsCourseAsync(courseId, instructorId, ct);
    }

    private static string? StatusReason(EnrollmentTrigger trigger, Enrollment enrollment) =>
        trigger == EnrollmentTrigger.Cancel ? enrollment.CancellationReason : enrollment.DecisionNote;

    private static CourseEnrollmentDto Map(Enrollment e, string studentName) => new()
    {
        Id = e.Id,
        StudentId = e.StudentId,
        StudentAppUserId = e.Student!.AppUserId,
        StudentName = studentName,
        CourseId = e.CourseId,
        CourseName = e.Course.Name,
        EnrollmentStatus = e.EnrollmentStatus,
        PaidUntil = e.PaidUntil,
        DecidedAt = e.DecidedAt,
        DecisionNote = e.DecisionNote,
        CanceledAt = e.CanceledAt,
        CancellationReason = e.CancellationReason
    };
}
