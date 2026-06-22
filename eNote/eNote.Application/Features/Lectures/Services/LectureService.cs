using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.Students;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ILogger<LectureService> logger,
    ICurrentUserService currentUserService,
    IMapper mapper) : ILectureService
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructor.Id, includeAttendances: true);
        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var entity = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .AsNoTracking()
            .ForEnrolledStudent(student.Id)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);

        var query = instructorAccess.LecturesFor(instructor.Id)
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(
            search.Page, search.PageSize, search.IncludeTotalCount,
            mapper.Map<LectureDto>,
            q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ForEnrolledStudent(student.Id)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(
            search.Page, search.PageSize, search.IncludeTotalCount,
            mapper.Map<LectureDto>,
            q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        await instructorAccess.EnsureOwnsCourseAsync(request.CourseId, instructor.Id);

        var entity = new Lecture(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.LectureType,
            request.Capacity,
            request.CourseId)
        {
            CreatedById = currentUserService.UserId
        };

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructor.Id, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.UpdateDetails(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.Capacity);
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructor.Id, track: true);

        entity.SoftDelete();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUserService.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructor.Id, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.Cancel();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

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

        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        if (!await context.IsEnrolledInCourseAsync(student.Id, lecture.CourseId))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == student.Id);

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
                lecture.Attendances.Add(new Attendance(student.Id, lecture.Id, AttendanceStatus.Present));
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

        return new RsvpResponse { LectureId = lecture.Id, StudentId = student.Id, Confirmed = request.Confirm };
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, int page, int pageSize)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructor.Id);

        var query = context.Set<Attendance>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.LectureId == lectureId);

        return await query.ToPagedResultAsync(
            page, pageSize, includeTotalCount: true,
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
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructor.Id, track: true, includeAttendances: true);

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
