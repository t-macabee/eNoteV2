using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    ICurrentUserContext currentUser, IStudentContext students,
    InstructorAccessService instructorAccess,
    ILectureNotificationDispatcher notificationDispatcher,
    IStudentDisplayNameService displayNames,
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
                StudentId = x.StudentId,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                CancellationReason = x.CancellationReason,
                CancelledAt = x.CancelledAt,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        await ApplyStudentNamesAsync([dto], cancellationToken);

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
                StudentId = x.StudentId,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                CancellationReason = x.CancellationReason,
                CancelledAt = x.CancelledAt,
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

        await ApplyStudentNamesAsync([dto], cancellationToken);

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
                StudentId = x.StudentId,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                CancellationReason = x.CancellationReason,
                CancelledAt = x.CancelledAt,
                LectureTime = x.LectureTime,
                Duration = x.Duration,
                Capacity = x.Capacity,
                AttendeeCount = x.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present)
            })
            .ToListAsync(cancellationToken);

        await ApplyStudentNamesAsync(items, cancellationToken);

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
                StudentId = x.StudentId,
                Name = x.Name,
                Location = x.Location,
                LectureType = x.LectureType,
                LectureStatus = x.LectureStatus,
                IsCancelled = x.LectureStatus == LectureStatus.Cancelled,
                CancellationReason = x.CancellationReason,
                CancelledAt = x.CancelledAt,
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

        await ApplyStudentNamesAsync(items, cancellationToken);

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

        if (request.StudentId.HasValue && !await context.Set<Enrollment>()
            .AnyAsync(e => e.StudentId == request.StudentId.Value && e.CourseId == request.CourseId && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken))
        {
            throw new BusinessException(Messages.LectureStudentNotActiveEnrollment);
        }

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
            request.CourseId,
            request.StudentId);

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        var created = mapper.Map<LectureDto>(entity);
        await ApplyStudentNamesAsync([created], cancellationToken);
        return created;
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

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<LectureDto>(entity);
        dto.AttendeeCount = await context.Set<Attendance>()
            .CountAsync(a => a.LectureId == id && a.AttendanceStatus == AttendanceStatus.Present, cancellationToken);
        await ApplyStudentNamesAsync([dto], cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id, LectureCancelRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true, cancellationToken: cancellationToken);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (entity.LectureStatus != LectureStatus.Scheduled)
        {
            throw new BusinessException(Messages.LectureAlreadyHeld);
        }

        entity.Cancel(request.Reason, currentUser.UserId, clock.UtcNow);

        var enrolledStudentUserIds = await context.Set<Enrollment>()
            .Where(e => e.CourseId == entity.CourseId
                && e.EnrollmentStatus == EnrollmentStatus.Active
                && (entity.StudentId == null || e.StudentId == entity.StudentId))
            .Select(e => e.Student.AppUserId)
            .ToListAsync(cancellationToken);

        await notificationDispatcher.DispatchCancelledAsync(entity.Id, entity.Name, entity.CancellationReason!, enrolledStudentUserIds);

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<LectureDto>(entity);
        dto.AttendeeCount = await context.Set<Attendance>()
            .CountAsync(a => a.LectureId == id && a.AttendanceStatus == AttendanceStatus.Present, cancellationToken);
        await ApplyStudentNamesAsync([dto], cancellationToken);
        return dto;
    }

    private async Task ApplyStudentNamesAsync(IEnumerable<LectureDto> items, CancellationToken cancellationToken)
    {
        List<LectureDto> individual = [.. items.Where(x => x.StudentId.HasValue)];

        if (individual.Count == 0)
        {
            return;
        }

        HashSet<int> studentIds = [.. individual.Select(x => x.StudentId!.Value)];

        var names = await displayNames.GetStudentDisplayNamesAsync(
            await context.Set<Student>().Where(s => studentIds.Contains(s.Id)).ToListAsync(cancellationToken),
            cancellationToken);

        foreach (var item in individual)
        {
            item.StudentName = names.GetValueOrDefault(item.StudentId!.Value, Messages.UnknownUserName);
        }
    }
}
