using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class InstructorEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser,
    InstructorAccessService instructorAccess,
    IStudentDisplayNameService displayNames,
    IEnrollmentNotificationDispatcher notificationDispatcher)
{
    public async Task<PagedResult<CourseEnrollmentDto>> GetForCourseAsync(int courseId, CourseEnrollmentSearchObject search, CancellationToken ct = default)
    {
        await EnsureOwnsCourseAsync(courseId, ct);

        var query = context.Set<Enrollment>()
            .AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId);

        if (search.EnrollmentStatus.HasValue)
        {
            query = query.Where(e => e.EnrollmentStatus == search.EnrollmentStatus.Value);
        }

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(ct) : (int?)null;

        var enrollments = await query
            .OrderBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var names = await displayNames.GetStudentDisplayNamesAsync(enrollments.Select(e => e.Student!), ct);

        return new PagedResult<CourseEnrollmentDto>
        {
            Items = [.. enrollments.Select(e => Map(e, names.GetValueOrDefault(e.StudentId, Messages.UnknownUserName)))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<CourseEnrollmentDto> ApproveAsync(int courseId, int enrollmentId, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Approve, reason: null, ct);

    public Task<CourseEnrollmentDto> RejectAsync(int courseId, int enrollmentId, EnrollmentRejectRequest request, CancellationToken ct = default) =>
        DecideAsync(courseId, enrollmentId, EnrollmentTrigger.Reject, request.Reason, ct);

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

        var result = enrollment.Transition(trigger, currentUser.UserId, clock.UtcNow, reason);

        if (!result.IsSuccess)
        {
            throw new BusinessException(result.Error);
        }

        await notificationDispatcher.DispatchStatusChangedAsync(enrollment.Id, enrollment.Student.AppUserId, enrollment.Course.Name, result.Value, enrollment.DecisionNote);

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

    private async Task EnsureOwnsCourseAsync(int courseId, CancellationToken ct)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, ct);
        await instructorAccess.EnsureOwnsCourseAsync(courseId, instructorId, ct);
    }

    private static CourseEnrollmentDto Map(Enrollment e, string studentName) => new()
    {
        Id = e.Id,
        StudentId = e.StudentId,
        StudentName = studentName,
        EnrollmentStatus = e.EnrollmentStatus,
        PaidUntil = e.PaidUntil,
        DecidedAt = e.DecidedAt,
        DecisionNote = e.DecisionNote
    };
}
