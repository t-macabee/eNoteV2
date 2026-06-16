using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Lectures.Search;
using eNote.Application.Features.Lectures.Services;
using eNote.Application.Features.Users.Services;

using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Lectures.Services
{
    public class LectureService(IAppDbContext context, IUserContextResolver resolver, ILogger<LectureService> logger, ICurrentUserService currentUserService) : ILectureService
    {
        public async Task<LectureDto> GetByIdForInstructorAsync(int id)
        {
            var entity = await GetLectureForInstructorAsync(id, currentUserService.UserId);

            return Map(entity);
        }

        public async Task<LectureDto> GetByIdForStudentAsync(int id)
        {
            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Course.IsPublished &&
                    x.LectureStatus != LectureStatus.Cancelled)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search)
        {
            var instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            var query = context.Set<Lecture>()
                .AsNoTracking()
                .Include(x => x.Attendances)
                .Where(x => x.Course.InstructorId == instructor.Id);

            query = ApplyFilters(query, search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, Map, q => q.OrderByDescending(x => x.LectureTime));
        }

        public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search)
        {
            var query = context.Set<Lecture>()
                .AsNoTracking()
                .Include(x => x.Attendances)
                .Where(x => x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled);

            query = ApplyFilters(query, search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, Map, q => q.OrderByDescending(x => x.LectureTime));
        }

        private static IQueryable<Lecture> ApplyFilters(IQueryable<Lecture> query, LectureSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.Name))
                query = query.Where(x => x.Name.Contains(search.Name));

            if (search.LectureType.HasValue)
                query = query.Where(x => x.LectureType == search.LectureType.Value);

            if (search.CourseId.HasValue)
                query = query.Where(x => x.CourseId == search.CourseId.Value);

            if (search.From.HasValue)
                query = query.Where(x => x.LectureTime >= search.From.Value);

            if (search.To.HasValue)
                query = query.Where(x => x.LectureTime <= search.To.Value);

            return query;
        }

        public async Task<LectureDto> CreateAsync(LectureCreateRequest request)
        {
            var courseId = request.CourseId ?? 0;

            if (courseId != 0)
            {
                _ = await context.Set<Course>()
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.Instructor.AppUserId == currentUserService.UserId)
                    ?? throw new AuthorizationException(Messages.CourseNotOwned);
            }

            var entity = new Lecture(
                request.Name.Trim(),
                request.Location.Trim(),
                request.Duration,
                request.LectureTime,
                request.LectureType,
                request.Capacity,
                request.CourseId ?? 0
            );

            entity.CreatedById = currentUserService.UserId;

            context.Set<Lecture>().Add(entity);
            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request)
        {
            var entity = await GetLectureForInstructorAsync(id, currentUserService.UserId, track: true);

            if (entity.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            entity.UpdateDetails(
                request.Name.Trim(),
                request.Location.Trim(),
                request.Duration,
                request.LectureTime,
                request.Capacity
            );
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetLectureForInstructorAsync(id, currentUserService.UserId, track: true);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUserService.UserId);
        }

        public async Task<LectureDto> CancelAsync(int id)
        {
            var entity = await GetLectureForInstructorAsync(id, currentUserService.UserId, track: true);

            if (entity.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            entity.Cancel();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return Map(entity);
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
                throw new BusinessException(Messages.LectureCancelled);

            var student = await resolver.GetStudentAsync(currentUserService.UserId);

            var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == student.Id);

            if (request.Confirm)
            {
                var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

                if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing == null || existing.AttendanceStatus != AttendanceStatus.Present))
                    throw new ConflictException(Messages.LectureFull);

                if (existing == null)
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
            await GetLectureForInstructorAsync(lectureId, currentUserService.UserId);

            var query = context.Set<Attendance>()
                .AsNoTracking()
                .Include(x => x.Student)
                .Where(x => x.LectureId == lectureId);

            return await query.ToPagedResultAsync(page, pageSize, includeTotalCount: true,
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
            var lecture = await GetLectureForInstructorAsync(lectureId, currentUserService.UserId, track: true);

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

        private async Task<Lecture> GetLectureForInstructorAsync(int lectureId, int userId, bool track = false)
        {
            var instructor = await resolver.GetInstructorAsync(userId);

            var query = context.Set<Lecture>()
                .Include(x => x.Attendances)
                .Where(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id);

            return await (track ? query : query.AsNoTracking()).FirstOrDefaultAsync()
                ?? throw new NotFoundException(Messages.LectureNotFound);
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
                LectureType = e.LectureType,
                LectureStatus = e.LectureStatus,
                IsCancelled = e.IsCancelled,
                AttendeeCount = e.Attendances?.Count(a => a.AttendanceStatus == AttendanceStatus.Present) ?? 0
            };
        }
    }
}