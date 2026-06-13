using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Courses.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Courses.Services
{
    public class CourseService(IAppDbContext context, ILogger<CourseService> logger) : ICourseService
    {
        public async Task<CourseDto> GetByIdAsync(int id, int requesterId)
        {
            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<CourseDto>> GetPagedAsync(int page, int pageSize, int requesterId)
        {
            (page, pageSize) = PagingLimits.Normalize(page, pageSize);

            var query = context.Set<Course>().AsNoTracking();

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var models = items.Select(Map).ToList();

            return new PagedResult<CourseDto>
            {
                Items = models,
                Page = page,
                PageSize = pageSize,
                TotalCount = await context.Set<Course>().CountAsync()
            };
        }

        public async Task<CourseDto> CreateAsync(int instructorUserId, CourseCreateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, instructorUserId);

            var entity = new Course
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsPublished = request.IsPublished,
                InstructorId = instructor.Id
            };

            context.Set<Course>().Add(entity);

            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, instructorUserId);

            return Map(entity);
        }

        public async Task EnrollAsync(int courseId, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var course = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == courseId)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            var existing = course.Enrollments
                .FirstOrDefault(e => e.StudentId == student.Id && e.EnrollmentStatus == Domain.Enums.EnrollmentStatus.Active);

            if (existing != null)
                return;

            var enrollment = new Enrollment
            {
                CourseId = course.Id,
                StudentId = student.Id,
                EnrollmentStatus = Domain.Enums.EnrollmentStatus.Active
            };

            context.Set<Enrollment>().Add(enrollment);

            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", studentUserId, courseId);
        }

        private static CourseDto Map(Course e)
        {
            return new CourseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Price = e.Price,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsPublished = e.IsPublished,
                EnrolledCount = e.Enrollments?.Count ?? 0,
                InstructorId = e.InstructorId
            };
        }
    }
}
