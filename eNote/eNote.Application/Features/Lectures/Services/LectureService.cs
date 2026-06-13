using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Lectures.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Lectures.Services
{
    public class LectureService(IAppDbContext context, ILogger<LectureService> logger) : ILectureService
    {
        public async Task<LectureDto> GetByIdAsync(int id, int requesterId)
        {
            var entity = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<LectureDto>> GetPagedAsync(int page, int pageSize, int requesterId)
        {
            (page, pageSize) = PagingLimits.Normalize(page, pageSize);

            var query = context.Set<Lecture>().AsNoTracking();

            var items = await query
                .OrderByDescending(x => x.LectureTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var models = items.Select(Map).ToList();

            return new PagedResult<LectureDto>
            {
                Items = models,
                Page = page,
                PageSize = pageSize,
                TotalCount = await context.Set<Lecture>().CountAsync()
            };
        }

        public async Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request)
        {
            var courseId = request.CourseId ?? 0;

            if (courseId != 0)
            {
                _ = await context.Set<Course>()
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.Instructor.AppUserId == teacherId)
                    ?? throw new AuthorizationException(Messages.CourseNotOwned);
            }

            var entity = new Lecture
            {
                Name = request.Name.Trim(),
                Location = request.Location.Trim(),
                LectureTime = request.LectureTime,
                Duration = request.Duration,
                Capacity = request.Capacity,
                LectureStatus = Domain.Enums.LectureStatus.Scheduled,
                IsCancelled = false,
                CourseId = request.CourseId ?? 0
            };

            context.Set<Lecture>().Add(entity);

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request)
        {
            var lecture = await context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == lectureId)
                ?? throw new NotFoundException(Messages.LectureNotFound);

            if (lecture.IsCancelled)
                throw new BusinessException(Messages.LectureCancelled);

            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == student.Id);

            if (request.Confirm)
            {
                var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == Domain.Enums.AttendanceStatus.Present);

                if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing == null || existing.AttendanceStatus != Domain.Enums.AttendanceStatus.Present))
                    throw new ConflictException(Messages.LectureFull);

                if (existing == null)
                {
                    lecture.Attendances.Add(new Attendance { StudentId = student.Id, LectureId = lecture.Id, AttendanceStatus = Domain.Enums.AttendanceStatus.Present });
                }
                else
                {
                    existing.AttendanceStatus = Domain.Enums.AttendanceStatus.Present;
                }
            }
            else
            {
                existing?.AttendanceStatus = Domain.Enums.AttendanceStatus.Absent;
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
                AttendeeCount = e.Attendances?.Count(a => a.AttendanceStatus == Domain.Enums.AttendanceStatus.Present) ?? 0
            };
        }
    }
}
