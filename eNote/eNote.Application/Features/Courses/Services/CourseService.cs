using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Courses.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Courses.Services
{
    public class CourseService(IAppDbContext context, IClock clock, ILogger<CourseService> logger) : ICourseService
    {
        public async Task<CourseDto> GetByIdForInstructorAsync(int id, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id && c.IsActive)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return Map(entity);
        }

        public async Task<CourseDto> GetByIdForStudentAsync(int id, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsActive &&
                    (c.IsPublished ||
                     c.Enrollments.Any(e =>
                         e.StudentId == student.Id &&
                         e.EnrollmentStatus == EnrollmentStatus.Active)))
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(int instructorUserId, int page, int pageSize)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == instructor.Id);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.StartDate));
        }

        public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(int page, int pageSize)
        {
            var query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.IsPublished);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.StartDate));
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
                InstructorId = instructor.Id,
                CreatedById = instructorUserId
            };

            context.Set<Course>().Add(entity);

            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, instructorUserId);

            return Map(entity);
        }

        public async Task<CourseDto> UpdateAsync(int id, int instructorUserId, CourseUpdateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id && c.IsActive)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.Name = request.Name.Trim();
            entity.Description = request.Description?.Trim();
            entity.Price = request.Price;
            entity.StartDate = request.StartDate;
            entity.EndDate = request.EndDate;
            entity.IsPublished = request.IsPublished;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int id, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id && c.IsActive)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.IsActive = false;
            entity.IsPublished = false;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            foreach (var lecture in entity.Lectures)
            {
                lecture.IsActive = false;
                lecture.UpdatedAt = clock.UtcNow;
                lecture.UpdatedById = instructorUserId;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, instructorUserId);
        }

        public async Task EnrollAsync(int courseId, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var course = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished && c.IsActive)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            var existing = course.Enrollments
                .FirstOrDefault(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active);

            if (existing != null)
                return;

            var enrollment = new Enrollment
            {
                CourseId = course.Id,
                StudentId = student.Id,
                EnrollmentStatus = EnrollmentStatus.Active
            };

            context.Set<Enrollment>().Add(enrollment);

            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", studentUserId, courseId);
        }

        public async Task UnenrollAsync(int courseId, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var enrollment = await context.Set<Enrollment>()
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.StudentId == student.Id &&
                    e.EnrollmentStatus == EnrollmentStatus.Active &&
                    e.Course.IsActive)
                ?? throw new BusinessException(Messages.StudentNotEnrolled);

            enrollment.EnrollmentStatus = EnrollmentStatus.Canceled;
            enrollment.UpdatedAt = clock.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", studentUserId, courseId);
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
                EnrolledCount = e.Enrollments?.Count(x => x.EnrollmentStatus == EnrollmentStatus.Active) ?? 0,
                InstructorId = e.InstructorId
            };
        }
    }
}
