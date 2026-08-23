using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureAttendanceService(IAppDbContext context, ICurrentActor actor, IStudentDisplayNameService displayNames, InstructorAccessService instructorAccess, ILogger<LectureAttendanceService> logger)
{
    public async Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request, CancellationToken cancellationToken = default)
    {
        var lecture = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled, cancellationToken) ?? throw new NotFoundException(Messages.LectureNotFound);

        var studentId = await actor.GetCurrentStudentIdAsync();

        if (!await context.IsEnrolledInCourseAsync(studentId, lecture.CourseId, cancellationToken))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == studentId);

        if (request.Confirm)
        {
            var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

            if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing is null || existing.AttendanceStatus != AttendanceStatus.Present))
            {
                throw new ConflictException(Messages.LectureFull);
            }

            if (existing is null)
            {
                lecture.Attendances.Add(new Attendance(studentId, lecture.Id, AttendanceStatus.Present));
            }
            else
            {
                existing.UpdateStatus(AttendanceStatus.Present);
            }
        }
        else
        {
            existing?.UpdateStatus(AttendanceStatus.Absent);
        }

        try
        {
            context.Set<Lecture>().Entry(lecture).State = EntityState.Modified;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while RSVPing for lecture {LectureId} by student user {StudentUserId}", lectureId, actor.UserId);
            throw new ConflictException(Messages.LectureRsvpConflict);
        }

        return new RsvpResponse { LectureId = lecture.Id, StudentId = studentId, Confirmed = request.Confirm };
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId, cancellationToken);

        var query = context.Set<Attendance>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.LectureId == lectureId);

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var attendances = await query
            .OrderBy(x => x.StudentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var names = await displayNames.GetStudentDisplayNamesAsync(attendances.Select(a => a.Student));

        return new PagedResult<AttendanceDto>
        {
            Items = [.. attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = names.GetValueOrDefault(a.StudentId, $"Student {a.StudentId}"),
                AttendanceStatus = a.AttendanceStatus
            })],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, track: true, includeAttendances: true, cancellationToken: cancellationToken);

        if (lecture.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (!await context.IsEnrolledInCourseAsync(request.StudentId, lecture.CourseId, cancellationToken))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var attendance = lecture.Attendances.FirstOrDefault(x => x.StudentId == request.StudentId);

        if (request.AttendanceStatus == AttendanceStatus.Present)
        {
            var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

            if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (attendance is null || attendance.AttendanceStatus != AttendanceStatus.Present))
            {
                throw new ConflictException(Messages.LectureFull);
            }
        }

        if (attendance is null)
        {
            attendance = new Attendance(request.StudentId, lecture.Id, request.AttendanceStatus)
            {
                CreatedById = actor.UserId
            };
            lecture.Attendances.Add(attendance);
        }
        else
        {
            attendance.UpdateStatus(request.AttendanceStatus);
            attendance.UpdatedById = actor.UserId;
        }

        await context.SaveChangesAsync(cancellationToken);

        var student = attendance.Student ?? await context.Set<Student>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == attendance.StudentId, cancellationToken);

        return new AttendanceDto
        {
            Id = attendance.Id,
            StudentId = attendance.StudentId,
            StudentName = await displayNames.GetStudentDisplayNameAsync(student),
            AttendanceStatus = attendance.AttendanceStatus
        };
    }
}
