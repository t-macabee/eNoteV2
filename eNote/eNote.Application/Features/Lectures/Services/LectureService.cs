using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Lectures.Services
{
    public class LectureService(IAppDbContext context) : ILectureService
    {
        private readonly IAppDbContext _context = context;

        public async Task<LectureDto> GetByIdAsync(int id, int requesterId)
        {
            var entity = await _context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Lecture not found");

            return Map(entity);
        }

        public async Task<PagedResult<LectureDto>> GetPagedAsync(int page, int pageSize, int requesterId)
        {
            var query = _context.Set<Lecture>().AsNoTracking();

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
                TotalCount = await _context.Set<Lecture>().CountAsync()
            };
        }

        public async Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request)
        {
            var courseId = request.CourseId ?? 0;

            if (courseId != 0)
            {
                var course = await _context.Set<Course>().FirstOrDefaultAsync(c => c.Id == courseId && c.Instructor.AppUserId == teacherId);
                if (course == null)
                    throw new UnauthorizedAccessException("You don't own the course specified.");
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

            _context.Set<Lecture>().Add(entity);
            await _context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request)
        {
            var lecture = await _context.Set<Lecture>()
                .Include(x => x.Attendances)
                .FirstOrDefaultAsync(x => x.Id == lectureId)
                ?? throw new KeyNotFoundException("Lecture not found");

            if (lecture.IsCancelled)
                throw new InvalidOperationException("Lecture is cancelled.");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.AppUserId == studentUserId) ?? throw new InvalidOperationException("Student profile not found.");

            var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == student.Id);

            if (request.Confirm)
            {
                var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == Domain.Enums.AttendanceStatus.Present);
                if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing == null || existing.AttendanceStatus != Domain.Enums.AttendanceStatus.Present))
                    throw new InvalidOperationException("Lecture is full.");

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
                if (existing != null)
                    existing.AttendanceStatus = Domain.Enums.AttendanceStatus.Absent;
            }

            await _context.SaveChangesAsync();

            var resp = new RsvpResponse { LectureId = lecture.Id, StudentId = student.Id, Confirmed = request.Confirm };
            return resp;
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
