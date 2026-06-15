using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Lectures.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Lectures.Services
{
    public class LectureService(IAppDbContext context, IClock clock, IUserIdentityService identity, ILogger<LectureService> logger) : ILectureService
    {
        public async Task<LectureDto> GetByIdForInstructorAsync(int id, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            return Map(entity);
        }

        public async Task<LectureDto> GetByIdForStudentAsync(int id, int studentUserId)
        {
            _ = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive &&
                    x.Course.IsActive &&
                    x.Course.IsPublished &&
                    !x.IsCancelled)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(int instructorUserId, int page, int pageSize)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var query = context.Set<Lecture>()
                .AsNoTracking()
                .Include(x => x.Attendances)
                .Where(x => x.Course.InstructorId == instructor.Id && x.IsActive);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.LectureTime));
        }

        public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(int page, int pageSize)
        {
            var query = context.Set<Lecture>()
                .AsNoTracking()
                .Include(x => x.Attendances)
                .Where(x => x.IsActive && x.Course.IsActive && x.Course.IsPublished && !x.IsCancelled);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.LectureTime));
        }

        public async Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request)
        {
            var courseId = request.CourseId ?? 0;

            if (courseId != 0)
            {
                _ = await context.Set<Course>()
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.Instructor.AppUserId == teacherId && c.IsActive)
                    ?? throw new AuthorizationException(Messages.CourseNotOwned);
            }

            var entity = new Lecture
            {
                Name = request.Name.Trim(),
                Location = request.Location.Trim(),
                LectureTime = request.LectureTime,
                Duration = request.Duration,
                Capacity = request.Capacity,
                LectureStatus = LectureStatus.Scheduled,
                IsCancelled = false,
                CourseId = request.CourseId ?? 0,
                CreatedById = teacherId
            };

            context.Set<Lecture>().Add(entity);

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<LectureDto> UpdateAsync(int id, int instructorUserId, LectureUpdateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            if (entity.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            entity.Name = request.Name.Trim();
            entity.Location = request.Location.Trim();
            entity.LectureTime = request.LectureTime;
            entity.Duration = request.Duration;
            entity.Capacity = request.Capacity;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int id, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Lecture>()
                .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            entity.IsActive = false;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, instructorUserId);
        }

        public async Task<LectureDto> CancelAsync(int id, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            if (entity.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            entity.IsCancelled = true;
            entity.LectureStatus = LectureStatus.Cancelled;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request)
        {
            var lecture = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x =>
                    x.Id == lectureId &&
                    x.IsActive &&
                    x.Course.IsActive &&
                    x.Course.IsPublished)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            if (lecture.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == student.Id);

            if (request.Confirm)
            {
                var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

                if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing == null || existing.AttendanceStatus != AttendanceStatus.Present))
                    throw new ConflictException(Messages.LectureFull);

                if (existing == null)
                {
                    lecture.Attendances.Add(new Attendance { StudentId = student.Id, LectureId = lecture.Id, AttendanceStatus = AttendanceStatus.Present });
                }
                else
                {
                    existing.AttendanceStatus = AttendanceStatus.Present;
                }
            }
            else
            {
                existing?.AttendanceStatus = AttendanceStatus.Absent;
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict while RSVPing for lecture {LectureId} by student user {StudentUserId}", lectureId, studentUserId);

                throw new ConflictException(Messages.LectureRsvpConflict);
            }

            return new RsvpResponse { LectureId = lecture.Id, StudentId = student.Id, Confirmed = request.Confirm };
        }

        public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, int instructorUserId, int page, int pageSize)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            var query = context.Set<Attendance>()
                .AsNoTracking()
                .Include(x => x.Student)
                .Where(x => x.LectureId == lectureId);

            (page, pageSize) = PagingLimits.Normalize(page, pageSize);

            var total = await query.CountAsync();
            var attendances = await query
                .OrderBy(x => x.StudentId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<AttendanceDto>();
            foreach (var attendance in attendances)
            {
                items.Add(new AttendanceDto
                {
                    Id = attendance.Id,
                    StudentId = attendance.StudentId,
                    StudentName = await UserProfileHelper.GetStudentDisplayNameAsync(identity, attendance.Student),
                    AttendanceStatus = attendance.AttendanceStatus
                });
            }

            return new PagedResult<AttendanceDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<AttendanceDto> MarkAttendanceAsync(int lectureId, int instructorUserId, MarkAttendanceRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var lecture = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            if (lecture.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            var isEnrolled = await context.Set<Enrollment>()
                .AsNoTracking()
                .AnyAsync(e =>
                    e.StudentId == request.StudentId &&
                    e.CourseId == lecture.CourseId &&
                    e.EnrollmentStatus == EnrollmentStatus.Active);

            if (!isEnrolled)
                throw new BusinessException(Messages.StudentNotEnrolled);

            var attendance = lecture.Attendances.FirstOrDefault(x => x.StudentId == request.StudentId);

            if (attendance is null)
            {
                attendance = new Attendance
                {
                    LectureId = lecture.Id,
                    StudentId = request.StudentId,
                    AttendanceStatus = request.AttendanceStatus,
                    CreatedById = instructorUserId
                };
                lecture.Attendances.Add(attendance);
            }
            else
            {
                attendance.AttendanceStatus = request.AttendanceStatus;
                attendance.UpdatedById = instructorUserId;
            }

            await context.SaveChangesAsync();

            var student = attendance.Student
                ?? await context.Set<Student>().AsNoTracking().FirstAsync(x => x.Id == attendance.StudentId);

            return new AttendanceDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = await UserProfileHelper.GetStudentDisplayNameAsync(identity, student),
                AttendanceStatus = attendance.AttendanceStatus
            };
        }

        private static LectureDto Map(Lecture e)
        {
            return new LectureDto
            {
                Id = e.Id,
                Name = e.Name,
                Location = e.Location,
                LectureTime = e.LectureTime,
                Duration = e.Duration,
                Capacity = e.Capacity,
                IsCancelled = e.IsCancelled,
                AttendeeCount = e.Attendances?.Count(a => a.AttendanceStatus == AttendanceStatus.Present) ?? 0
            };
        }
    }
}
