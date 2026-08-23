using eNote.Application.Features.Identity.Instructors;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    ICurrentUserContext currentUser, IStudentContext students,
    InstructorAccessService instructorAccess,
    ILectureNotificationDispatcher notificationDispatcher,
    ILogger<LectureService> logger,
    IMapper mapper)
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, includeAttendances: true, cancellationToken: cancellationToken);
        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync();

        var entity = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var query = instructorAccess.LecturesFor(instructorId)
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime), cancellationToken);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync();

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime), cancellationToken);
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        await instructorAccess.EnsureOwnsCourseAsync(request.CourseId, instructorId, cancellationToken);

        var location = request.Location.Trim().ToLower();

        if (await context.Set<Lecture>().HasLocationConflictAsync(location, request.LectureTime, request.Duration, cancellationToken: cancellationToken) ||
            await context.Set<Lecture>().HasInstructorConflictAsync(instructorId, request.LectureTime, request.Duration, cancellationToken: cancellationToken))
        {
            throw new ConflictException(Messages.LectureTimeConflict);
        }

        var entity = new Lecture(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.LectureType,
            request.Capacity,
            request.CourseId)
        {
            CreatedById = currentUser.UserId
        };

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (request.Capacity.HasValue)
        {
            var confirmedCount = await context.Set<Attendance>()
                .CountAsync(a => a.LectureId == id && a.AttendanceStatus == AttendanceStatus.Present, cancellationToken);

            if (request.Capacity.Value < confirmedCount)
            {
                throw new ConflictException(Messages.LectureCapacityBelowConfirmed);
            }
        }

        var location = request.Location.Trim().ToLower();

        if (await context.Set<Lecture>().HasLocationConflictAsync(location, request.LectureTime, request.Duration, id, cancellationToken) ||
            await context.Set<Lecture>().HasInstructorConflictAsync(instructorId, request.LectureTime, request.Duration, id, cancellationToken))
        {
            throw new ConflictException(Messages.LectureTimeConflict);
        }

        entity.UpdateDetails(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.Capacity);
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.Cancel();
        entity.UpdatedById = currentUser.UserId;

        var enrolledStudentUserIds = await context.Set<Enrollment>()
            .Where(e => e.CourseId == entity.CourseId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(e => e.Student.AppUserId)
            .ToListAsync(cancellationToken);

        await notificationDispatcher.DispatchCancelledAsync(entity.Id, entity.Name, enrolledStudentUserIds);

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<LectureDto>(entity);
    }
}
