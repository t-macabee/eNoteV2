using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Courses.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Courses.Services
{
    public class CourseService(IAppDbContext context, ICurrentUserService currentUserService, ILogger<CourseService> logger) : ICourseService
    {
        public async Task<CourseDto> GetByIdForInstructorAsync(int id)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return Map(entity);
        }

        public async Task<CourseDto> GetByIdForStudentAsync(int id)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    (c.IsPublished ||
                     c.Enrollments.Any(e =>
                         e.StudentId == student.Id &&
                         e.EnrollmentStatus == EnrollmentStatus.Active)))
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return Map(entity);
        }

        public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(int page, int pageSize)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == instructor.Id);

            return await query.ToPagedResultAsync(page, pageSize, includeTotalCount: true, Map, q => q.OrderByDescending(x => x.StartDate));
        }

        public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(int page, int pageSize)
        {
            var query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.IsPublished);

            return await query.ToPagedResultAsync(page, pageSize, includeTotalCount: true, Map, q => q.OrderByDescending(x => x.StartDate));
        }

        public async Task<CourseDto> CreateAsync(CourseCreateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, currentUserService.UserId);

            var entity = new Course(
                request.Name.Trim(),
                request.Description?.Trim(),
                request.Price,
                request.StartDate,
                request.EndDate,
                instructor.Id
            );
            entity.SetPublishedStatus(request.IsPublished);
            entity.CreatedById = currentUserService.UserId;

            context.Set<Course>().Add(entity);
            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, currentUserService.UserId);

            return Map(entity);
        }

        public async Task<CourseDto> UpdateAsync(int id, CourseUpdateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.UpdateDetails(
                request.Name.Trim(),
                request.Description?.Trim(),
                request.Price,
                request.StartDate,
                request.EndDate
            );
            entity.SetPublishedStatus(request.IsPublished);
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var entity = await context.Set<Course>()
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            foreach (var lecture in entity.Lectures)
            {
                lecture.SoftDelete();
                lecture.UpdatedById = currentUserService.UserId;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, currentUserService.UserId);
        }

        public async Task EnrollAsync(int courseId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId);

            var course = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            var existing = course.Enrollments.FirstOrDefault(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active);

            if (existing != null) return;

            var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);

            context.Set<Enrollment>().Add(enrollment);
            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", currentUserService.UserId, courseId);
        }

        public async Task UnenrollAsync(int courseId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId);

            var enrollment = await context.Set<Enrollment>()
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.StudentId == student.Id &&
                    e.EnrollmentStatus == EnrollmentStatus.Active)
                ?? throw new BusinessException(Messages.StudentNotEnrolled);

            enrollment.UpdateStatus(EnrollmentStatus.Canceled);

            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", currentUserService.UserId, courseId);
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