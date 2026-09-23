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
    IMapper mapper,
    IClock clock)
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var dto = await context.Set<Lecture>()
            .AsNoTracking()
            .Where(x => x.Id == id && x.Course.InstructorId == instructorId)
            .Select(x => new LectureDto
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return dto;
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var dto = await context.Set<Lecture>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId, clock.UtcNow)
            .Where(x => x.Id == id)
            .Select(x => new LectureDto
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present),
                MyAttendanceStatus = x.Attendances
                    .Where(a => a.StudentId == studentId)
                    .Select(a => (AttendanceStatus?)a.AttendanceStatus)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return dto;
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var query = instructorAccess.LecturesFor(instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        int? total = null;
        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var items = await query
            .OrderByDescending(x => x.LectureTime)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LectureDto
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LectureDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId, clock.UtcNow)
            .ApplySearch(search);

        int? total = null;
        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var items = await query
            .OrderByDescending(x => x.LectureTime)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LectureDto
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present),
                MyAttendanceStatus = x.Attendances
                    .Where(a => a.StudentId == studentId)
                    .Select(a => (AttendanceStatus?)a.AttendanceStatus)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LectureDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
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
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
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

        var dto = mapper.Map<LectureDto>(entity);
        dto.AttendeeCount = await context.Set<Attendance>()
            .CountAsync(a => a.LectureId == id && a.AttendanceStatus == AttendanceStatus.Present, cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (entity.LectureStatus == LectureStatus.Held)
        {
            throw new BusinessException(Messages.LectureAlreadyHeld);
        }

        entity.Cancel();
        entity.UpdatedById = currentUser.UserId;

        var enrolledStudentUserIds = await context.Set<Enrollment>()
            .Where(e => e.CourseId == entity.CourseId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(e => e.Student.AppUserId)
            .ToListAsync(cancellationToken);

        await notificationDispatcher.DispatchCancelledAsync(entity.Id, entity.Name, enrolledStudentUserIds);

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<LectureDto>(entity);
        dto.AttendeeCount = await context.Set<Attendance>()
            .CountAsync(a => a.LectureId == id && a.AttendanceStatus == AttendanceStatus.Present, cancellationToken);
        return dto;
    }
}
