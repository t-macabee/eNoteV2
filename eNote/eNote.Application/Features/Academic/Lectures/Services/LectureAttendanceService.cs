using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Students;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureAttendanceService(
    IAppDbContext context,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ILogger<LectureAttendanceService> logger,
    ICurrentUserService currentUserService) : ILectureAttendanceService
{
    public async Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request)
    {
        var lecture = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x =>
                x.Id == lectureId &&
                x.Course.IsPublished &&
                x.LectureStatus != LectureStatus.Cancelled)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        if (lecture.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        var studentId = await resolver.GetCurrentStudentIdAsync(currentUserService.UserId);

        if (!await context.IsEnrolledInCourseAsync(studentId, lecture.CourseId))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == studentId);

        if (request.Confirm)
        {
            var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

            if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value &&
                (existing is null || existing.AttendanceStatus != AttendanceStatus.Present))
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
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while RSVPing for lecture {LectureId} by student user {StudentUserId}", lectureId, currentUserService.UserId);
            throw new ConflictException(Messages.LectureRsvpConflict);
        }

        return new RsvpResponse { LectureId = lecture.Id, StudentId = studentId, Confirmed = request.Confirm };
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var query = context.Set<Attendance>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.LectureId == lectureId);

        return await query.ToPagedResultAsync(
            search,
            items => resolver.GetStudentDisplayNamesAsync(items.Select(a => a.Student)),
            (a, names) => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = names.GetValueOrDefault(a.StudentId, $"Student {a.StudentId}"),
                AttendanceStatus = a.AttendanceStatus
            },
            q => q.OrderBy(x => x.StudentId));
    }

    public async Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, track: true, includeAttendances: true);

        if (lecture.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (!await context.IsEnrolledInCourseAsync(request.StudentId, lecture.CourseId))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var attendance = lecture.Attendances.FirstOrDefault(x => x.StudentId == request.StudentId);

        if (attendance is null)
        {
            attendance = new Attendance(request.StudentId, lecture.Id, request.AttendanceStatus)
            {
                CreatedById = currentUserService.UserId
            };
            lecture.Attendances.Add(attendance);
        }
        else
        {
            attendance.UpdateStatus(request.AttendanceStatus);
            attendance.UpdatedById = currentUserService.UserId;
        }

        await context.SaveChangesAsync();

        var student = attendance.Student ?? await context.Set<Student>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == attendance.StudentId);

        return new AttendanceDto
        {
            Id = attendance.Id,
            StudentId = attendance.StudentId,
            StudentName = await resolver.GetStudentDisplayNameAsync(student),
            AttendanceStatus = attendance.AttendanceStatus
        };
    }
}
